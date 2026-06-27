using Arrowgene.Ddon.Server.Scripting.utils;
using System.ComponentModel;

namespace Arrowgene.Ddon.Server.Settings
{
    public class ChatCommandSettings : IGameSettings
    {
        public ChatCommandSettings(ScriptableSettings settingsData) : base(settingsData, typeof(ChatCommandSettings).Name)
        {
        }

        /// <summary>
        /// If set to true, disables the account type checks for all chat commands.
        /// </summary>
        [DefaultValue(_DisableAccountTypeCheck)]
        public bool DisableAccountTypeCheck
        {
            set
            {
                SetSetting("DisableAccountTypeCheck", value);
            }
            get
            {
                return TryGetSetting("DisableAccountTypeCheck", _DisableAccountTypeCheck);
            }
        }
        private const bool _DisableAccountTypeCheck = false;

        /// <summary>
        /// Controls whether Shout is sent to all channels or not.
        /// </summary>
        [DefaultValue(_CrossChannelShout)]
        public bool CrossChannelShout
        {
            set
            {
                SetSetting("CrossChannelShout", value);
            }
            get
            {
                return TryGetSetting("CrossChannelShout", _CrossChannelShout);
            }
        }
        private const bool _CrossChannelShout = true;
    }
}
