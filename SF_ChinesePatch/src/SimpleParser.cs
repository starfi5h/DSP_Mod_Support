using System.Collections.Generic;
using System.Text;

namespace SF_ChinesePatch
{
    // Modify from https://stackoverflow.com/a/13192508 
    public class SimpleParser
    {
        public static Dictionary<string, string> Parse(string json)
        {
            Dictionary<string, string> dict = new ();
            bool inquotes = false;
            string key = null;
            StringBuilder sb = new ();
            for (int i = 0; i < json.Length; i++)
            {
                char c = json[i];

                if (c == '"')
                {
                    inquotes = !inquotes;
                    continue;
                }
                if (inquotes)
                {
                    sb.Append(c);
                }
                else
                {
                    switch (c)
                    {
                        case ',':
                            if (key != null)
                            {
                                dict.Add(key, sb.ToString());
                                key = null;
                                sb.Length = 0;
                            }
                            continue;
                        case ':':
                            key = sb.ToString();
                            sb.Length = 0;
                            continue;
                    }
                }
            }
            return dict;
        }
    }
}
