﻿/*
 * This file is subject to the terms and conditions defined in
 * file 'license.txt', which is part of this source code package.
 */



using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace SteamKit2
{
    /// <summary>
    /// This handler handles user stat related actions.
    /// </summary>
    public sealed partial class SteamUserStats : ClientMsgHandler
    {

        /// <summary>
        /// Retrieves the number of current players or a given <see cref="GameID"/>.
        /// Results are returned in a <see cref="NumberOfPlayersCallback"/> from a <see cref="JobCallback"/>.
        /// </summary>
        /// <param name="gameId">The GameID to request the number of players for.</param>
        /// <returns>The Job ID of the request. This can be used to find the appropriate <see cref="JobCallback"/>.</returns>
        public ulong GetNumberOfCurrentPlayers( GameID gameId )
        {
            var msg = new ClientMsg<MsgClientGetNumberOfCurrentPlayers>();
            msg.SourceJobID = Client.GetNextJobID();

            msg.Body.GameID = gameId;

            Client.Send( msg );

            return msg.SourceJobID;
        }


        /// <summary>
        /// Handles a client message. This should not be called directly.
        /// </summary>
        /// <param name="e">The <see cref="SteamKit2.ClientMsgEventArgs"/> instance containing the event data.</param>
        public override void HandleMsg( IPacketMsg packetMsg )
        {
            switch ( packetMsg.MsgType )
            {
                case EMsg.ClientGetNumberOfCurrentPlayersResponse:
                    HandleNumberOfPlayersResponse( packetMsg );
                    break;
            }
        }


        #region ClientMsg Handlers
        void HandleNumberOfPlayersResponse( IPacketMsg packetMsg )
        {
            var msg = new ClientMsg<MsgClientGetNumberOfCurrentPlayersResponse>( packetMsg );
#if STATIC_CALLBACKS
            var innerCallback = new NumberOfPlayersCallback( Client, msg.Body );
            var callback = new SteamClient.JobCallback<NumberOfPlayersCallback>( Client, msg.Header.TargetJobID, innerCallback );
            SteamClient.PostCallback( callback );
#else
            var innerCallback = new NumberOfPlayersCallback( msg.Body );
            var callback = new SteamClient.JobCallback<NumberOfPlayersCallback>( msg.Header.TargetJobID, innerCallback );
            Client.PostCallback( callback );
#endif
        }
        #endregion
    }
}