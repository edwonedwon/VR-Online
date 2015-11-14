/*-----------------------------+------------------------------\
|                                                             |
|                        !!!NOTICE!!!                         |
|                                                             |
|  These libraries are under heavy development so they are    |
|  subject to make many changes as development continues.     |
|  For this reason, the libraries may not be well commented.  |
|  THANK YOU for supporting forge with all your feedback      |
|  suggestions, bug reports and comments!                     |
|                                                             |
|                               - The Forge Team              |
|                                 Bearded Man Studios, Inc.   |
|                                                             |
|  This source code, project files, and associated files are  |
|  copyrighted by Bearded Man Studios, Inc. (2012-2015) and   |
|  may not be redistributed without written permission.       |
|                                                             |
\------------------------------+-----------------------------*/



namespace BeardedManStudios.Network
{
	/// <summary>
	/// This is often used in conjunction with RPC or WriteCustom in order to limit who gets the call
	/// </summary>
	public enum NetworkReceivers
	{
		All = 0,
		AllBuffered = 1,
		Others = 2,
		OthersBuffered = 3,
		Server = 4,
		AllProximity = 5,
		OthersProximity = 6,
		Owner = 7,
		MessageGroup = 8
	}
}