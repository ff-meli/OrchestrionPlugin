using Dalamud.Plugin;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace OrchestrionPlugin
{
    [StructLayout(LayoutKind.Sequential)]
    unsafe struct BGMPlayback
    {
        public int priorityIndex;
        public int unk1;   // often has a value, not sure what it is
        public int unk2;   // seemingly always 0
        // often writing songId will cause songId2 and 3 to be written automatically
        // songId3 is sometimes not updated at all, and I'm unsure of its use
        // zeroing out songId2 seems to be necessary to actually cancel playback without using
        // an invalid id (which is the only way to do it with just songId1)
        public ushort songId;               // Sometimes the id, sometimes something unclear.  Maybe some kind of flags? Values seem all over the place
        public ushort songId2;              // Seems to be the 'actual' song that is playing, when not 0 or 1
        public ushort songId3;              // Usually seems to match the current actual song, but not define it...; may not have reset issues like songId2
        public byte timerEnable;            // whether the timer automatically counts up
        public byte padding;
        public float timer;                 // if enabled, seems to always count from 0 to 6
        // if 0x30 is 0, up through 0x4F are 0
        // in theory function params can be written here if 0x30 is non-zero but I've never seen it
        public fixed byte unk3[56];
        public byte blockTimer;             // if the timer has expired, the song will stop if this byte is 0; if it is 1, the song will be locked on until this changes
        public fixed byte unk4[3];
        public short unk5;                  // may interact with the timer, seems to break it sometimes; set to 0x100 for priority 0 in some cases but seems to not do anything
        public fixed byte unk6[2];
    }

    class BGMRecord
    {
        public int priority;
        public ushort songId;
        // timestamp?

        // because I am lazy
        public void Set(int priority, ushort songId)
        {
            this.priority = priority;
            this.songId = songId;
        }
    }

    class BGMControl : IDisposable
    {
        public ushort CurrentSongId { get; private set; }

        public delegate void SongChangedHandler(ushort newSongId);
        public SongChangedHandler OnSongChanged;

        // this seems to always be the number of bgm blocks that exist
        private const int ControlBlockCount = 12;

        private AddressResolver Address { get; }
        private CancellationTokenSource cancellationToken;
        private BGMRecord previousSongInfo = new BGMRecord();
        public bool shuffleEnabled;
        public List<int> playlist { get; set; }

        public System.Timers.Timer timer { get; set; }

        public BGMControl(AddressResolver address)
        {
            this.Address = address;
            this.cancellationToken = new CancellationTokenSource();
        }

        public void Dispose()
        {
            this.cancellationToken.Cancel();
        }

        public void StartUpdate()
        {
            timer = new System.Timers.Timer(180000);
            timer.Elapsed += async (sender, e) => Shuffle(12);
            timer.AutoReset = true;

            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    if (this.cancellationToken.IsCancellationRequested)
                    {
                        //throw new OperationCanceledException();
                        break;
                    }

                    this.Update();

                    await Task.Delay(2000);
                }
            }, cancellationToken.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private void Update()
        {
            var currentSong = (ushort)0;
            var activePriority = 0;

            this.Address.UpdateBGMControl();
            if (this.Address.BGMControl != IntPtr.Zero)
            {
                unsafe
                {
                    var bgms = (BGMPlayback*)this.Address.BGMControl.ToPointer();
                    

                    // as far as I have seen, the control blocks are in priority order
                    // and the highest priority populated song is what the client current plays
                    for (activePriority = 0; activePriority < ControlBlockCount; activePriority++)
                    {
                        PluginLog.Log("//////////////////");
                        PluginLog.Log(bgms[activePriority].songId.ToString());
                        PluginLog.Log(bgms[activePriority].songId2.ToString());
                        PluginLog.Log(bgms[activePriority].songId3.ToString());
                        PluginLog.Log(bgms[activePriority].priorityIndex.ToString());
                        PluginLog.Log(bgms[activePriority].timer.ToString());
                        PluginLog.Log(bgms[activePriority].timer.ToString());
                        PluginLog.Log(bgms[activePriority].timerEnable.ToString());
                        PluginLog.Log(bgms[activePriority].unk1.ToString());
                        PluginLog.Log(bgms[activePriority].unk2.ToString());
                        PluginLog.Log(bgms[activePriority].unk5.ToString());
                        PluginLog.Log(bgms[activePriority].blockTimer.ToString());

                        PluginLog.Log("//////////////////");

                        // TODO: everything here is awful and makes me sad

                        // This value isn't a collection of flags, but it seems like if it's 0 entirely, the song at this
                        // priority isn't playing
                        // Earlying out here since the checks below are already awful enough
                        if (bgms[activePriority].songId == 0)
                        {
                            continue;
                        }

                        // reading songId2 here, because occasionally songId is something different
                        // not sure of what the meaning is when that is the case
                        // eg, songId2 and songId3 are 0x7, which is correct, but songId was 0x3EB
                        // We could also read songId3, but there are cases where it is set despite not playing
                        // (eg, user disables mount music, songId and songId2 will be 0, but songId3 will be the non-playing mount song)

                        // An attempt to deal with the weird "fighting" sound issue, which results in a lot of incorrect spam
                        // of song changes.
                        // Often with overlaid zone music (beast tribes, festivals, etc), prio 10 will be the actual music the user
                        // hears, but it will very quickly reset songId2 to 0 and then back, while songId3 doesn't change.
                        // This leads to song transition messages to the prio 11 zone music and then back to the overlaid prio 10 music
                        // over and over again, despite the actual music being played not changing.
                        if (activePriority == previousSongInfo.priority && bgms[activePriority].songId2 == 0
                            && previousSongInfo.songId != 0 && bgms[activePriority].songId3 == previousSongInfo.songId)
                        {
#if DEBUG
                            PluginLog.Log("skipping change from {0} to {1} on prio {2}", previousSongInfo.songId, bgms[activePriority].songId2, activePriority);
#endif
                            return;
                        }


                        // TODO: might want to have a method to check if an id is valid, in case there are other weird cases
                        if (bgms[activePriority].songId2 != 0 && bgms[activePriority].songId2 != 9999)
                        {
                            currentSong = bgms[activePriority].songId2;
                            break;
                        }
                    }
                }
            }

            PluginLog.Log("currentsong " + currentSong.ToString());

            if (MemoryUtil.GetMusicWriteAddress() == 0 && shuffleEnabled && timer.Enabled)
            {

                this.Shuffle(activePriority);

                return;
            }

            // separate variable because 0 is valid if nothing is playing
            if (CurrentSongId != currentSong)
            {


#if DEBUG
                PluginLog.Log($"changed to song {currentSong} at priority {activePriority}");
#endif
                CurrentSongId = currentSong;

                OnSongChanged?.Invoke(currentSong);

                previousSongInfo.Set(activePriority, currentSong);
            }
        }

        // priority ranges from 0 to ControlBlockCount-1, with lower values overriding higher ones
        // so in theory, priority 0 should override everything else
        public void SetSong(ushort songId, int priority = 0)
        {
            if (priority < 0 || priority >= ControlBlockCount)
            {
                throw new IndexOutOfRangeException();
            }

            this.Address.UpdateBGMControl();
            if (this.Address.BGMControl != IntPtr.Zero)
            {
                unsafe
                {
                    var bgms = (BGMPlayback*)this.Address.BGMControl.ToPointer();
                    // sometimes we only have to set the first and it will set the other 2
                    // but particularly on stop/clear, the 2nd seems important as well
                    bgms[priority].songId = songId;
                    bgms[priority].songId2 = songId;
                    bgms[priority].songId3 = songId;

                    // these are probably not necessary, but clear them to be safe
                    bgms[priority].timer = 0;
                    bgms[priority].timerEnable = 0;

                    // unk5 is set to 0x100 by the game in some cases for priority 0
                    // but I wasn't able to see that it did anything
                }
            }
        }

        public unsafe void DumpPriorityInfo()
        {
            PluginLog.Log("----- BGM priority dump -----");

            this.Address.UpdateBGMControl();
            if (this.Address.BGMControl != IntPtr.Zero)
            {
                var bgms = (BGMPlayback*)this.Address.BGMControl.ToPointer();

                for (int prio = 0; prio < ControlBlockCount; prio++)
                {
                    PluginLog.Log($"  Priority {prio}, songId {bgms[prio].songId}, songId2 {bgms[prio].songId2}, songId3 {bgms[prio].songId3}");
                }
            }
            else
            {
                PluginLog.Log("BGMControl was null");
            }

            PluginLog.Log("----- BGM dump done -----");
        }

        private void Shuffle(int activePriority)
        {
            timer.Stop();
            var index = this.playlist.FindIndex(x => x == CurrentSongId);

            var nextSong = (ushort)this.playlist[index + 1];
#if DEBUG
            PluginLog.Log($"shuffling to song {nextSong} at priority {activePriority}");

            CurrentSongId = nextSong;
#endif
            OnSongChanged?.Invoke(nextSong);

            previousSongInfo.Set(activePriority, nextSong);

            this.SetSong(nextSong, 0);

            while (MemoryUtil.GetMusicWriteAddress() == 0)
            {
                Thread.Sleep(25);
            }
            timer.Start();
        }
    }
}
