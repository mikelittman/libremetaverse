/*
 * Copyright (c) 2006-2016, openmetaverse.co
 * All rights reserved.
 *
 * - Redistribution and use in source and binary forms, with or without
 *   modification, are permitted provided that the following conditions are met:
 *
 * - Redistributions of source code must retain the above copyright notice, this
 *   list of conditions and the following disclaimer.
 * - Neither the name of the openmetaverse.co nor the names
 *   of its contributors may be used to endorse or promote products derived from
 *   this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 * POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using OpenMetaverse.Packets;

namespace OpenMetaverse
{
    /// <summary>
    /// 
    /// </summary>
    public class SoundManager
    {
        #region Private Members
        private readonly GridClient _client;
        #endregion

        #region Event Handling
        /// <summary>The event subscribers, null of no subscribers</summary>
        private EventHandler<AttachedSoundEventArgs> _mAttachedSound;

        ///<summary>Raises the AttachedSound Event</summary>
        /// <param name="e">A AttachedSoundEventArgs object containing
        /// the data sent from the simulator</param>
        protected virtual void OnAttachedSound(AttachedSoundEventArgs e)
        {
            EventHandler<AttachedSoundEventArgs> handler = _mAttachedSound;
            handler?.Invoke(this, e);
        }

        /// <summary>Thread sync lock object</summary>
        private readonly object _mAttachedSoundLock = new object();

        /// <summary>Raised when the simulator sends us data containing
        /// sound</summary>
        public event EventHandler<AttachedSoundEventArgs> AttachedSound
        {
            add { lock (_mAttachedSoundLock) { _mAttachedSound += value; } }
            remove { lock (_mAttachedSoundLock) { _mAttachedSound -= value; } }
        }
                
        /// <summary>The event subscribers, null of no subscribers</summary>
        private EventHandler<AttachedSoundGainChangeEventArgs> m_AttachedSoundGainChange;

        ///<summary>Raises the AttachedSoundGainChange Event</summary>
        /// <param name="e">A AttachedSoundGainChangeEventArgs object containing
        /// the data sent from the simulator</param>
        protected virtual void OnAttachedSoundGainChange(AttachedSoundGainChangeEventArgs e)
        {
            EventHandler<AttachedSoundGainChangeEventArgs> handler = m_AttachedSoundGainChange;
            handler?.Invoke(this, e);
        }

        /// <summary>Thread sync lock object</summary>
        private readonly object m_AttachedSoundGainChangeLock = new object();

        /// <summary>Raised when the simulator sends us data containing
        /// ...</summary>
        public event EventHandler<AttachedSoundGainChangeEventArgs> AttachedSoundGainChange
        {
            add { lock (m_AttachedSoundGainChangeLock) { m_AttachedSoundGainChange += value; } }
            remove { lock (m_AttachedSoundGainChangeLock) { m_AttachedSoundGainChange -= value; } }
        }
        
        /// <summary>The event subscribers, null of no subscribers</summary>
        private EventHandler<SoundTriggerEventArgs> m_SoundTrigger;

        ///<summary>Raises the SoundTrigger Event</summary>
        /// <param name="e">A SoundTriggerEventArgs object containing
        /// the data sent from the simulator</param>
        protected virtual void OnSoundTrigger(SoundTriggerEventArgs e)
        {
            EventHandler<SoundTriggerEventArgs> handler = m_SoundTrigger;
            handler?.Invoke(this, e);
        }

        /// <summary>Thread sync lock object</summary>
        private readonly object m_SoundTriggerLock = new object();

        /// <summary>Raised when the simulator sends us data containing
        /// ...</summary>
        public event EventHandler<SoundTriggerEventArgs> SoundTrigger
        {
            add { lock (m_SoundTriggerLock) { m_SoundTrigger += value; } }
            remove { lock (m_SoundTriggerLock) { m_SoundTrigger -= value; } }
        }

        /// <summary>The event subscribers, null of no subscribers</summary>
        private EventHandler<PreloadSoundEventArgs> m_PreloadSound;

        ///<summary>Raises the PreloadSound Event</summary>
        /// <param name="e">A PreloadSoundEventArgs object containing
        /// the data sent from the simulator</param>
        protected virtual void OnPreloadSound(PreloadSoundEventArgs e)
        {
            EventHandler<PreloadSoundEventArgs> handler = m_PreloadSound;
            handler?.Invoke(this, e);
        }

        /// <summary>Thread sync lock object</summary>
        private readonly object m_PreloadSoundLock = new object();

        /// <summary>Raised when the simulator sends us data containing
        /// ...</summary>
        public event EventHandler<PreloadSoundEventArgs> PreloadSound
        {
            add { lock (m_PreloadSoundLock) { m_PreloadSound += value; } }
            remove { lock (m_PreloadSoundLock) { m_PreloadSound -= value; } }
        }

        #endregion

        /// <summary>
        /// Construct a new instance of the SoundManager class, used for playing and receiving
        /// sound assets
        /// </summary>
        /// <param name="client">A reference to the current GridClient instance</param>
        public SoundManager(GridClient client)
        {
            _client = client;
            
            _client.Network.RegisterCallback(PacketType.AttachedSound, AttachedSoundHandler);
            _client.Network.RegisterCallback(PacketType.AttachedSoundGainChange, AttachedSoundGainChangeHandler);
            _client.Network.RegisterCallback(PacketType.PreloadSound, PreloadSoundHandler);
            _client.Network.RegisterCallback(PacketType.SoundTrigger, SoundTriggerHandler);
        }

        #region public methods

        /// <summary>
        /// Plays a sound in the current region at full volume from avatar position
        /// </summary>
        /// <param name="soundID">UUID of the sound to be played</param>
        public void PlaySound(UUID soundID)
        {
            SendSoundTrigger(soundID, _client.Self.SimPosition, 1.0f);
        }

        /// <summary>
        /// Plays a sound in the current region at full volume
        /// </summary>
        /// <param name="soundID">UUID of the sound to be played.</param>
        /// <param name="position">position for the sound to be played at. Normally the avatar.</param>
        public void SendSoundTrigger(UUID soundID, Vector3 position)
        {
            SendSoundTrigger(soundID, _client.Self.SimPosition, 1.0f);
        }

        /// <summary>
        /// Plays a sound in the current region
        /// </summary>
        /// <param name="soundID">UUID of the sound to be played.</param>
        /// <param name="position">position for the sound to be played at. Normally the avatar.</param>
        /// <param name="gain">volume of the sound, from 0.0 to 1.0</param>
        public void SendSoundTrigger(UUID soundID, Vector3 position, float gain)
        {
            SendSoundTrigger(soundID, _client.Network.CurrentSim.Handle, position, gain);
        }
        /// <summary>
        /// Plays a sound in the specified sim
        /// </summary>
        /// <param name="soundID">UUID of the sound to be played.</param>
        /// <param name="sim">UUID of the sound to be played.</param>
        /// <param name="position">position for the sound to be played at. Normally the avatar.</param>
        /// <param name="gain">volume of the sound, from 0.0 to 1.0</param>
        public void SendSoundTrigger(UUID soundID, Simulator sim, Vector3 position, float gain)
        {
            SendSoundTrigger(soundID, sim.Handle, position, gain);
        }

        /// <summary>
        /// Play a sound asset
        /// </summary>
        /// <param name="soundID">UUID of the sound to be played.</param>
        /// <param name="handle">handle id for the sim to be played in.</param>
        /// <param name="position">position for the sound to be played at. Normally the avatar.</param>
        /// <param name="gain">volume of the sound, from 0.0 to 1.0</param>
        public void SendSoundTrigger(UUID soundID, ulong handle, Vector3 position, float gain)
        {
            SoundTriggerPacket soundtrigger = new SoundTriggerPacket
            {
                SoundData = new SoundTriggerPacket.SoundDataBlock
                {
                    SoundID = soundID,
                    ObjectID = UUID.Zero,
                    OwnerID = UUID.Zero,
                    ParentID = UUID.Zero,
                    Handle = handle,
                    Position = position,
                    Gain = gain
                }
            };

            _client.Network.SendPacket(soundtrigger);
        }

        #endregion
        #region Packet Handlers


        /// <summary>Process an incoming packet and raise the appropriate events</summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">The EventArgs object containing the packet data</param>
        protected void AttachedSoundHandler(object sender, PacketReceivedEventArgs e)
        {
            if (_mAttachedSound == null) return;

            AttachedSoundPacket sound = (AttachedSoundPacket)e.Packet;
            OnAttachedSound(new AttachedSoundEventArgs(e.Simulator, sound.DataBlock.SoundID, sound.DataBlock.OwnerID,
                sound.DataBlock.ObjectID, sound.DataBlock.Gain, (SoundFlags)sound.DataBlock.Flags));
        }

        /// <summary>Process an incoming packet and raise the appropriate events</summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">The EventArgs object containing the packet data</param>
        protected void AttachedSoundGainChangeHandler(object sender, PacketReceivedEventArgs e)
        {
            if (m_AttachedSoundGainChange == null) return;

            AttachedSoundGainChangePacket change = (AttachedSoundGainChangePacket)e.Packet;
            OnAttachedSoundGainChange(new AttachedSoundGainChangeEventArgs(e.Simulator, change.DataBlock.ObjectID,
                change.DataBlock.Gain));
        }

        /// <summary>Process an incoming packet and raise the appropriate events</summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">The EventArgs object containing the packet data</param>
        protected void PreloadSoundHandler(object sender, PacketReceivedEventArgs e)
        {
            if (m_PreloadSound == null) return;

            PreloadSoundPacket preload = (PreloadSoundPacket)e.Packet;
            foreach (var data in preload.DataBlock)
            {
                OnPreloadSound(new PreloadSoundEventArgs(e.Simulator, data.SoundID, data.OwnerID, data.ObjectID));
            }
        }

        /// <summary>Process an incoming packet and raise the appropriate events</summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">The EventArgs object containing the packet data</param>
        protected void SoundTriggerHandler(object sender, PacketReceivedEventArgs e)
        {
            if (m_SoundTrigger == null) return;

            SoundTriggerPacket trigger = (SoundTriggerPacket)e.Packet;
            OnSoundTrigger(new SoundTriggerEventArgs(e.Simulator,
                trigger.SoundData.SoundID,
                trigger.SoundData.OwnerID,
                trigger.SoundData.ObjectID,
                trigger.SoundData.ParentID,
                trigger.SoundData.Gain,
                trigger.SoundData.Handle,
                trigger.SoundData.Position));
        }
        
        #endregion
    }
    #region EventArgs

    /// <summary>Provides data for the <see cref="SoundManager.AttachedSound"/> event</summary>
    /// <remarks>The <see cref="SoundManager.AttachedSound"/> event occurs when the simulator sends
    /// the sound data which emits from an agents attachment</remarks>
    /// <example>
    /// The following code example shows the process to subscribe to the <see cref="SoundManager.AttachedSound"/> event
    /// and a stub to handle the data passed from the simulator
    /// <code>
    ///     // Subscribe to the AttachedSound event
    ///     Client.Sound.AttachedSound += Sound_AttachedSound;
    ///     
    ///     // process the data raised in the event here
    ///     private void Sound_AttachedSound(object sender, AttachedSoundEventArgs e)
    ///     {
    ///         // ... Process AttachedSoundEventArgs here ...
    ///     }
    /// </code>
    /// </example>
    public class AttachedSoundEventArgs : EventArgs
    {
        /// <summary>Simulator where the event originated</summary>
        public Simulator Simulator { get; }

        /// <summary>Get the sound asset id</summary>
        public UUID SoundID { get; }

        /// <summary>Get the ID of the owner</summary>
        public UUID OwnerID { get; }

        /// <summary>Get the ID of the Object</summary>
        public UUID ObjectID { get; }

        /// <summary>Get the volume level</summary>
        public float Gain { get; }

        /// <summary>Get the <see cref="SoundFlags"/></summary>
        public SoundFlags Flags { get; }

        /// <summary>
        /// Construct a new instance of the SoundTriggerEventArgs class
        /// </summary>
        /// <param name="sim">Simulator where the event originated</param>
        /// <param name="soundID">The sound asset id</param>
        /// <param name="ownerID">The ID of the owner</param>
        /// <param name="objectID">The ID of the object</param>
        /// <param name="gain">The volume level</param>
        /// <param name="flags">The <see cref="SoundFlags"/></param>
        public AttachedSoundEventArgs(Simulator sim, UUID soundID, UUID ownerID, UUID objectID, float gain, SoundFlags flags)
        {
            Simulator = sim;
            SoundID = soundID;
            OwnerID = ownerID;
            ObjectID = objectID;
            Gain = gain;
            Flags = flags;
        }
    }

    /// <summary>Provides data for the <see cref="SoundManager.AttachedSoundGainChange"/> event</summary>
    /// <remarks>The <see cref="SoundManager.AttachedSoundGainChange"/> event occurs when an attached sound
    /// changes its volume level</remarks>
    public class AttachedSoundGainChangeEventArgs : EventArgs
    {
        /// <summary>Simulator where the event originated</summary>
        public Simulator Simulator { get; }

        /// <summary>Get the ID of the Object</summary>
        public UUID ObjectID { get; }

        /// <summary>Get the volume level</summary>
        public float Gain { get; }

        /// <summary>
        /// Construct a new instance of the AttachedSoundGainChangedEventArgs class
        /// </summary>
        /// <param name="sim">Simulator where the event originated</param>
        /// <param name="objectID">The ID of the Object</param>
        /// <param name="gain">The new volume level</param>
        public AttachedSoundGainChangeEventArgs(Simulator sim, UUID objectID, float gain)
        {
            Simulator = sim;
            ObjectID = objectID;
            Gain = gain;
        }
    }

    /// <summary>Provides data for the <see cref="SoundManager.SoundTrigger"/> event</summary>
    /// <remarks><para>The <see cref="SoundManager.SoundTrigger"/> event occurs when the simulator forwards
    /// a request made by yourself or another agent to play either an asset sound or a built in sound</para>
    /// 
    /// <para>Requests to play sounds where the <see cref="SoundTriggerEventArgs.SoundID"/> is not one of the built-in
    /// <see cref="Sounds"/> will require sending a request to download the sound asset before it can be played</para>
    /// </remarks>
    /// <example>
    /// The following code example uses the <see cref="SoundTriggerEventArgs.OwnerID"/>, <see cref="SoundTriggerEventArgs.SoundID"/> 
    /// and <see cref="SoundTriggerEventArgs.Gain"/>
    /// properties to display some information on a sound request on the <see cref="Console"/> window.
    /// <code>
    ///     // subscribe to the event
    ///     Client.Sound.SoundTrigger += Sound_SoundTrigger;
    ///
    ///     // play the pre-defined BELL_TING sound
    ///     Client.Sound.SendSoundTrigger(Sounds.BELL_TING);
    ///     
    ///     // handle the response data
    ///     private void Sound_SoundTrigger(object sender, SoundTriggerEventArgs e)
    ///     {
    ///         Console.WriteLine("{0} played the sound {1} at volume {2}",
    ///             e.OwnerID, e.SoundID, e.Gain);
    ///     }    
    /// </code>
    /// </example>
    public class SoundTriggerEventArgs : EventArgs
    {
        /// <summary>Simulator where the event originated</summary>
        public Simulator Simulator { get; }

        /// <summary>Get the sound asset id</summary>
        public UUID SoundID { get; }

        /// <summary>Get the ID of the owner</summary>
        public UUID OwnerID { get; }

        /// <summary>Get the ID of the Object</summary>
        public UUID ObjectID { get; }

        /// <summary>Get the ID of the objects parent</summary>
        public UUID ParentID { get; }

        /// <summary>Get the volume level</summary>
        public float Gain { get; }

        /// <summary>Get the regionhandle</summary>
        public ulong RegionHandle { get; }

        /// <summary>Get the source position</summary>
        public Vector3 Position { get; }

        /// <summary>
        /// Construct a new instance of the SoundTriggerEventArgs class
        /// </summary>
        /// <param name="sim">Simulator where the event originated</param>
        /// <param name="soundID">The sound asset id</param>
        /// <param name="ownerID">The ID of the owner</param>
        /// <param name="objectID">The ID of the object</param>
        /// <param name="parentID">The ID of the objects parent</param>
        /// <param name="gain">The volume level</param>
        /// <param name="regionHandle">The regionhandle</param>
        /// <param name="position">The source position</param>
        public SoundTriggerEventArgs(Simulator sim, UUID soundID, UUID ownerID, UUID objectID, UUID parentID, float gain, ulong regionHandle, Vector3 position)
        {
            Simulator = sim;
            SoundID = soundID;
            OwnerID = ownerID;
            ObjectID = objectID;
            ParentID = parentID;
            Gain = gain;
            RegionHandle = regionHandle;
            Position = position;
        }
    }

    /// <summary>Provides data for the <see cref="AvatarManager.AvatarAppearance"/> event</summary>
    /// <remarks>The <see cref="AvatarManager.AvatarAppearance"/> event occurs when the simulator sends
    /// the appearance data for an avatar</remarks>
    /// <example>
    /// The following code example uses the <see cref="AvatarAppearanceEventArgs.AvatarID"/> and <see cref="AvatarAppearanceEventArgs.VisualParams"/>
    /// properties to display the selected shape of an avatar on the <see cref="Console"/> window.
    /// <code>
    ///     // subscribe to the event
    ///     Client.Avatars.AvatarAppearance += Avatars_AvatarAppearance;
    /// 
    ///     // handle the data when the event is raised
    ///     void Avatars_AvatarAppearance(object sender, AvatarAppearanceEventArgs e)
    ///     {
    ///         Console.WriteLine("The Agent {0} is using a {1} shape.", e.AvatarID, (e.VisualParams[31] &gt; 0) : "male" ? "female")
    ///     }
    /// </code>
    /// </example>
    public class PreloadSoundEventArgs : EventArgs
    {
        /// <summary>Simulator where the event originated</summary>
        public Simulator Simulator { get; }

        /// <summary>Get the sound asset id</summary>
        public UUID SoundID { get; }

        /// <summary>Get the ID of the owner</summary>
        public UUID OwnerID { get; }

        /// <summary>Get the ID of the Object</summary>
        public UUID ObjectID { get; }

        /// <summary>
        /// Construct a new instance of the PreloadSoundEventArgs class
        /// </summary>
        /// <param name="sim">Simulator where the event originated</param>
        /// <param name="soundID">The sound asset id</param>
        /// <param name="ownerID">The ID of the owner</param>
        /// <param name="objectID">The ID of the object</param>
        public PreloadSoundEventArgs(Simulator sim, UUID soundID, UUID ownerID, UUID objectID)
        {
            Simulator = sim;
            SoundID = soundID;
            OwnerID = ownerID;
            ObjectID = objectID;
        }
    }
    #endregion
}