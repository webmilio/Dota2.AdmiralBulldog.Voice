using System.Collections.Generic;

namespace Webmilio.Dota2.AdmiralBulldog
{
    // Possibly remove, depending on wether or not the software makes a webserver.
    public struct CustomSoundRequest
    {
        public string @event;
        public Dictionary<string, string> data;

        public CustomSoundRequest(string @event, Dictionary<string, string> data)
        {
            this.@event = @event;
            this.data = data;
        }

        public CustomSoundRequest(string @event, string key, string value)
        {
            this.@event = @event;
            this.data = new Dictionary<string, string>() { { key, value } };
        }
    }
}
