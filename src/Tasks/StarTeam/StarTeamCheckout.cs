//
// NAntContrib
// Copyright (C) 2002 Tomas Restrepo (tomasr@mvps.org)
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307 USA
//

using System;
using System.IO;
using SourceForge.NAnt;
using SourceForge.NAnt.Attributes;
using InterOpStarTeam = StarTeam;

namespace NAnt.Contrib.Tasks.StarTeam 
{
	/// <summary>
	/// Task to check out files from StarTeam repositories. 
	/// </summary>
	/// <remarks>
	/// <para>You can check out by <see cref="label"/> and control the type of lock with <see cref="locktype"/>.</para>
	/// <para>You can delete files that are not in source control by setting <see cref="deleteuncontrolled" />.</para>
	/// <para>This task was ported from the Ant task http://jakarta.apache.org/ant/manual/OptionalTasks/starteam.html#stcheckout </para>
	/// <para>You need to have the StarTeam SDK installed for this task to function correctly.</para>
	/// </remarks>
	/// <example>
	///   <para>Recursively checks out all files in the project with an exclusive lock.</para>
	///   <code><![CDATA[
	///		<!-- 
	///		constructs a 'url' containing connection information to pass to the task 
	///		alternatively you can set each attribute manually 
	///		-->
	///		<property name="ST.url" value="user:pass@serverhost:49201/projectname/viewname"/>
	///		<stcheckout locktype="exclusive" rootstarteamfolder="/" recursive="true" url="${ST.url}" />
	///   ]]></code>
	/// </example>
	[TaskName("stcheckout")]
	public class StarTeamCheckout : TreeBasedTask
	{
		private InterOpStarTeam.StItem_LockTypeStaticsClass starTeamLockTypeStatics;
		private InterOpStarTeam.StStatusStaticsClass starTeamStatusStatics;
		public StarTeamCheckout()
		{
			starTeamLockTypeStatics = new InterOpStarTeam.StItem_LockTypeStaticsClass(); 
			starTeamStatusStatics = new InterOpStarTeam.StStatusStaticsClass();
			_lockStatus = starTeamLockTypeStatics.UNCHANGED; 
		}
		/// <summary> 
		/// Default : true - Create directories that are in the Starteam repository even if they are empty.
		/// </summary>
		[TaskAttribute("createworkingdirs", Required=false)]
		[BooleanValidator]         
		public virtual bool createworkingdirs
		{
			set { _createDirs = value; }			
		}

		/// <summary> 
		/// Default : false - Should all local files <b>NOT</b> in StarTeam be deleted?
		/// </summary>
		[TaskAttribute("deleteuncontrolled", Required=false)]
		[BooleanValidator]         
		public virtual bool deleteuncontrolled
		{
			set { _deleteUncontrolled = value; }			
		}
		
		/// <summary> 
		/// Label used for checkout. If no label is set latest state of repository is checked out.
		/// </summary>
		/// <remarks>
		/// The label must exist in starteam or an exception will be thrown. 
		/// </remarks>
		[TaskAttribute("label", Required=false)]
		[BooleanValidator]         
		public virtual string label
		{
			set { _setLabel(value); }			
		}

		/// <summary> 
		/// What type of lock to apply to files checked out. 
		/// <list type="bullet">
		/// 	<listheader>
		///			<term>LockType</term>
		/// 	</listheader>
		/// 	<item>
		///			<term>unchanged</term>
		/// 		<description>default: do not make any changes to the lock state of items.</description>
		///		</item>
		/// 	<item>
		///			<term>exclusive</term>
		/// 		<description>Exclusively lock items. No other users can update the object while it is exclusively locked.</description>
		///		</item>
		/// 	<item>
		///			<term>nonexclusive</term>
		/// 		<description>Put a non-exclusive lock on the item.</description>
		///		</item>
		/// 	<item>
		///			<term>unlocked</term>
		/// 		<description>Remove locks from all items checked out. This accompanied by force would effectively override a lock and replace local contents with the current version.</description>
		///		</item>
		/// </list>
		/// </summary>
		[TaskAttribute("locktype")]
		public virtual string locktype
		{
			set 
			{
				switch(value.ToLower())
				{
					case "unchanged": 
					default: 
						_lockStatus = starTeamLockTypeStatics.UNCHANGED; break;
					case "exclusive" :
						_lockStatus = starTeamLockTypeStatics.EXCLUSIVE; break;
					case "nonexclusive" :
						_lockStatus = starTeamLockTypeStatics.NONEXCLUSIVE; break;
					case "unlocked" :
						_lockStatus = starTeamLockTypeStatics.UNLOCKED; break;
				}
			}			
		}
		
		/// <value> holder for the createDirs property.</value>
		private bool _createDirs = true;
		
		/// <value> holder for the deleteUncontrolled property.</value>
		private bool _deleteUncontrolled = false;
		
		/// <value> holder for the lockstatus property. </value>
		private int _lockStatus;
		
		/// <summary> 
		/// Override of base-class abstract function creates an appropriately configured view for checkout. 
		/// If a label is specified it is used otherwise the current view of the repository is used.
		/// </summary>
		/// <param name="raw">the unconfigured <code>StarTeam View</code></param>
		/// <returns>the snapshot <code>StarTeam View</code> appropriately configured.</returns>
		protected override internal InterOpStarTeam.StView createSnapshotView(InterOpStarTeam.StView raw)
		{
			InterOpStarTeam.StViewConfigurationStaticsClass starTeamViewConfiguration = new InterOpStarTeam.StViewConfigurationStaticsClass();
			InterOpStarTeam.StViewFactory starTeamViewFactory = new InterOpStarTeam.StViewFactory();
			int labelID = getLabelID(raw);
		
			// if a label has been supplied, use it to configure the view
			// otherwise use current view
			if (labelID >= 0)
			{
				return starTeamViewFactory.Create(raw, starTeamViewConfiguration.createFromLabel(labelID));
			}
			else
			{
				return starTeamViewFactory.Create(raw, starTeamViewConfiguration.createTip());
			}
		}
	
		/// <summary> Implements base-class abstract function to define tests for any preconditons required by the task</summary>
		protected internal override void  testPreconditions()
		{
			if (null != _rootLocalFolder && !this.forced)
			{
				Log.WriteLine("Warning: rootLocalFolder specified, but forcing off.");
			}
		}
		
		/// <summary> 
		/// Implements base-class abstract function to perform the checkout operation on the files in each folder of the tree.
		/// </summary>
		/// <param name="starteamFolder">the StarTeam folder from which files to be checked out</param>
		/// <param name="targetFolder">the local mapping of the starteam folder</param>
		protected internal override void visit(InterOpStarTeam.StFolder starteamFolder, FileInfo targetFolder)
		{           
   			int notProcessed = 0;

			try
			{
				System.Collections.Hashtable localFiles = listLocalFiles(targetFolder);
				
				// If we have been told to create the working folders
				if (_createDirs)
				{
					// Create if it doesn't exist
					bool tmpBool;
					if (File.Exists(targetFolder.FullName))
						tmpBool = true;
					else
						tmpBool = Directory.Exists(targetFolder.FullName);
					if (!tmpBool)
					{
						Directory.CreateDirectory(targetFolder.FullName);
					}
				}
				// For all Files in this folder, we need to check
				// if there have been modifications.
				foreach(InterOpStarTeam.StFile stFile in starteamFolder.getItems("File"))
				{
					string filename = stFile.Name;
					FileInfo localFile = new FileInfo(Path.Combine(targetFolder.FullName, filename));
				
					delistLocalFile(localFiles, localFile);
					
					// If the file doesn't pass the include/exclude tests, skip it.
//					if (!shouldProcess(filename))
//					{
//						Log.WriteLine("Skipping : {0}",eachFile.toString());
//						continue;
//					}
					
					
					// If forced is not set then we may save ourselves some work by
					// looking at the status flag.
					// Otherwise, we care nothing about these statuses.
					
					if (!this.forced)
					{
						int fileStatus = (stFile.Status);
						
						// We try to update the status once to give StarTeam
						// another chance.
						if (fileStatus == starTeamStatusStatics.merge || fileStatus == starTeamStatusStatics.UNKNOWN)
						{
							stFile.updateStatus(true, true);
							fileStatus = (stFile.Status);
						}
						if (fileStatus == starTeamStatusStatics.CURRENT)
						{
							//count files not processed so we can 
							notProcessed++;
							//Log.WriteLine("Not processing {0} as it is current.", stFile.toString());
							continue;
						}
					}
					
					// Check out anything else.
					// Just a note: StarTeam has a status for NEW which implies
					// that there is an item  on your local machine that is not
					// in the repository.  These are the items that show up as
					// NOT IN VIEW in the Starteam GUI.
					// One would think that we would want to perhaps checkin the
					// NEW items (not in all cases! - Steve Cohen 15 Dec 2001)
					// Unfortunately, the sdk doesn't really work, and we can't
					// actually see  anything with a status of NEW. That is why
					// we can just check out  everything here without worrying
					// about losing anything.
					
					if(this.Verbose) 
					{
						Log.WriteLine("Checking Out: " + (localFile.ToString()));
					}
					stFile.checkoutTo(localFile.FullName, _lockStatus, true, true, true);
					_filesAffected++;
				}
				
				if(this.Verbose && notProcessed > 0)
				{
					Log.WriteLine("{0} : {1} files not processed because they were current.",targetFolder.FullName,notProcessed.ToString());
				}

				// Now we recursively call this method on all sub folders in this
				// folder unless recursive attribute is off.
				foreach(InterOpStarTeam.StFolder stFolder in starteamFolder.SubFolders)
				{
					FileInfo targetSubfolder = new FileInfo(Path.Combine(targetFolder.FullName, stFolder.Name));
					delistLocalFile(localFiles, targetSubfolder);
					if (this.recursive)
					{
						visit(stFolder, targetSubfolder);
					}
				}
				
				if (_deleteUncontrolled)
				{
					deleteUncontrolledItems(localFiles);
				}
				
			}
			catch (IOException e)
			{
				throw new BuildException(e.Message);
			}
		}
		                                                         		
		/// <summary> 
		/// Deletes everything on the local machine that is not in the repository.
		/// </summary>
		/// <param name="localFiles">Hashtable containing filenames to be deleted</param>
		private void  deleteUncontrolledItems(System.Collections.Hashtable localFiles)
		{
			try
			{
				foreach(string fileName in localFiles.Keys)
				{
					FileInfo file = new FileInfo(fileName);
					delete(file);
				}
			}
			//TODO: Move security catch into delete()
			catch (System.Security.SecurityException e)
			{
				Log.WriteLine("Error deleting file: {0}", e);
			}
		}
		
		/// <summary> Utility method to delete the file (and it's children) from the local drive.</summary>
		/// <param name="file">the file or directory to delete.</param>
		/// <returns>was the file successfully deleted</returns>
		private bool delete(FileInfo file)
		{
			// If the current file is a Directory, we need to delete all
			// of its children as well.
			if (Directory.Exists(file.FullName))
			{
				foreach(FileInfo dirfiles in file.Directory.GetFiles())
				{
					delete(dirfiles);
				}
			}
			
			Log.WriteLine("Deleting: {0}",file.FullName);
			bool tmpBool;
			if (File.Exists(file.FullName))
			{
				File.Delete(file.FullName);
				tmpBool = true;
			}
			else if (Directory.Exists(file.FullName))
			{
				Directory.Delete(file.FullName);
				tmpBool = true;
			}
			else
				tmpBool = false;
			return tmpBool;
		}
	}
}