using Dalamud.Game;
using Dalamud.Game.Internal;
using Dalamud.Plugin;
using System;
using System.Runtime.InteropServices;

namespace OrchestrionPlugin
{
    class AddressResolver : BaseAddressResolver
    {
        public IntPtr BaseAddress { get; private set; }
        public IntPtr BGMControl { get; private set; }

        protected override void Setup64Bit(SigScanner sig)
        {
            // TODO: this is probably on framework or gui somewhere, which might be cleaner if that is exposed
            this.BaseAddress = sig.GetStaticAddressFromSig("48 8B 05 ?? ?? ?? ?? 48 85 C0 74 42 83 78 08 0A", 3);

            UpdateBGMControl();
        }

        public void UpdateBGMControl()
        {
            var baseObject = Marshal.ReadIntPtr(this.BaseAddress);
            // I've never seen this happen, but the game checks for it in a number of places
            if (baseObject != IntPtr.Zero)
            {
                this.BGMControl = Marshal.ReadIntPtr(baseObject + 0xC0);
                PluginLog.Log(this.BGMControl.ToString());

            }
            else
            {
                this.BGMControl = IntPtr.Zero;
            }
        }
    }
}
