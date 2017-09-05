using ParseFive.Extensions;
using DOCUMENT_MODE = HTML.DOCUMENT_MODE;
// ReSharper disable InconsistentNaming

namespace ParseFive.Common
{
    static class Doctype
    {
        //var DOCUMENT_MODE = require('./html').DOCUMENT_MODE;
        //Const
        const string VALID_DOCTYPE_NAME = "html";
        const string QUIRKS_MODE_SYSTEM_ID = "http://www.ibm.com/data/dtd/v11/ibmxhtml1-transitional.dtd";
        static readonly string[] QUIRKS_MODE_PUBLIC_ID_PREFIXES;
        static readonly string[] QUIRKS_MODE_NO_SYSTEM_ID_PUBLIC_ID_PREFIXES;
        static readonly string[] QUIRKS_MODE_PUBLIC_IDS;
        static readonly string[] LIMITED_QUIRKS_PUBLIC_ID_PREFIXES;
        static readonly string[] LIMITED_QUIRKS_WITH_SYSTEM_ID_PUBLIC_ID_PREFIXES;


        static Doctype() {
            QUIRKS_MODE_PUBLIC_ID_PREFIXES = new[] {
                "+//silmaril//dtd html pro v0r11 19970101//en",
                "-//advasoft ltd//dtd html 3.0 aswedit + extensions//en",
                "-//as//dtd html 3.0 aswedit + extensions//en",
                "-//ietf//dtd html 2.0 level 1//en",
                "-//ietf//dtd html 2.0 level 2//en",
                "-//ietf//dtd html 2.0 strict level 1//en",
                "-//ietf//dtd html 2.0 strict level 2//en",
                "-//ietf//dtd html 2.0 strict//en",
                "-//ietf//dtd html 2.0//en",
                "-//ietf//dtd html 2.1e//en",
                "-//ietf//dtd html 3.0//en",
                "-//ietf//dtd html 3.0//en//",
                "-//ietf//dtd html 3.2 final//en",
                "-//ietf//dtd html 3.2//en",
                "-//ietf//dtd html 3//en",
                "-//ietf//dtd html level 0//en",
                "-//ietf//dtd html level 0//en//2.0",
                "-//ietf//dtd html level 1//en",
                "-//ietf//dtd html level 1//en//2.0",
                "-//ietf//dtd html level 2//en",
                "-//ietf//dtd html level 2//en//2.0",
                "-//ietf//dtd html level 3//en",
                "-//ietf//dtd html level 3//en//3.0",
                "-//ietf//dtd html strict level 0//en",
                "-//ietf//dtd html strict level 0//en//2.0",
                "-//ietf//dtd html strict level 1//en",
                "-//ietf//dtd html strict level 1//en//2.0",
                "-//ietf//dtd html strict level 2//en",
                "-//ietf//dtd html strict level 2//en//2.0",
                "-//ietf//dtd html strict level 3//en",
                "-//ietf//dtd html strict level 3//en//3.0",
                "-//ietf//dtd html strict//en",
                "-//ietf//dtd html strict//en//2.0",
                "-//ietf//dtd html strict//en//3.0",
                "-//ietf//dtd html//en",
                "-//ietf//dtd html//en//2.0",
                "-//ietf//dtd html//en//3.0",
                "-//metrius//dtd metrius presentational//en",
                "-//microsoft//dtd internet explorer 2.0 html strict//en",
                "-//microsoft//dtd internet explorer 2.0 html//en",
                "-//microsoft//dtd internet explorer 2.0 tables//en",
                "-//microsoft//dtd internet explorer 3.0 html strict//en",
                "-//microsoft//dtd internet explorer 3.0 html//en",
                "-//microsoft//dtd internet explorer 3.0 tables//en",
                "-//netscape comm. corp.//dtd html//en",
                "-//netscape comm. corp.//dtd strict html//en",
                "-//o\"reilly and associates//dtd html 2.0//en",
                "-//o\"reilly and associates//dtd html extended 1.0//en",
                "-//spyglass//dtd html 2.0 extended//en",
                "-//sq//dtd html 2.0 hotmetal + extensions//en",
                "-//sun microsystems corp.//dtd hotjava html//en",
                "-//sun microsystems corp.//dtd hotjava strict html//en",
                "-//w3c//dtd html 3 1995-03-24//en",
                "-//w3c//dtd html 3.2 draft//en",
                "-//w3c//dtd html 3.2 final//en",
                "-//w3c//dtd html 3.2//en",
                "-//w3c//dtd html 3.2s draft//en",
                "-//w3c//dtd html 4.0 frameset//en",
                "-//w3c//dtd html 4.0 transitional//en",
                "-//w3c//dtd html experimental 19960712//en",
                "-//w3c//dtd html experimental 970421//en",
                "-//w3c//dtd w3 html//en",
                "-//w3o//dtd w3 html 3.0//en",
                "-//w3o//dtd w3 html 3.0//en//",
                "-//webtechs//dtd mozilla html 2.0//en",
                "-//webtechs//dtd mozilla html//en"
            };

            QUIRKS_MODE_NO_SYSTEM_ID_PUBLIC_ID_PREFIXES = QUIRKS_MODE_PUBLIC_ID_PREFIXES.concat(new[] {
                "-//w3c//dtd html 4.01 frameset//",
                "-//w3c//dtd html 4.01 transitional//"
            });

            QUIRKS_MODE_PUBLIC_IDS = new[] {
                "-//w3o//dtd w3 html strict 3.0//en//",
                "-/w3c/dtd html 4.0 transitional/en",
                "html"
            };

            LIMITED_QUIRKS_PUBLIC_ID_PREFIXES = new[] {
                "-//W3C//DTD XHTML 1.0 Frameset//",
                "-//W3C//DTD XHTML 1.0 Transitional//"
            };

            LIMITED_QUIRKS_WITH_SYSTEM_ID_PUBLIC_ID_PREFIXES = LIMITED_QUIRKS_PUBLIC_ID_PREFIXES.concat(new[] {
                "-//W3C//DTD HTML 4.01 Frameset//",
                "-//W3C//DTD HTML 4.01 Transitional//"
            });
        }
        

        static string enquoteDoctypeId(string id)
        {
            char quote = id.indexOf('"') != -1
                         ? '\\'
                         : '"';
            return quote + id + quote;
        }

        static bool hasPrefix(string publicId, string [] prefixes)
        {
            for (var i = 0; i < prefixes.Length; i++)
            {
                if (publicId.indexOf(prefixes[i]) == 0)
                    return true;
                
            }
            return false;
        }

        public static string getDocumentMode(string name, string publicId, string systemId)
        {
            if (name != VALID_DOCTYPE_NAME)
                return DOCUMENT_MODE.QUIRKS;
            
            if (systemId != null && systemId.toLowerCase() == QUIRKS_MODE_SYSTEM_ID)
                return DOCUMENT_MODE.QUIRKS;

            if (publicId != null)
            {
                publicId = publicId.toLowerCase();

                if (QUIRKS_MODE_PUBLIC_IDS.indexOf(publicId) > -1)
                    return DOCUMENT_MODE.QUIRKS;

                string[] prefixes = systemId == null ? QUIRKS_MODE_NO_SYSTEM_ID_PUBLIC_ID_PREFIXES : QUIRKS_MODE_PUBLIC_ID_PREFIXES;

                if (hasPrefix(publicId, prefixes))
                    return DOCUMENT_MODE.QUIRKS;

                prefixes = systemId == null ? LIMITED_QUIRKS_PUBLIC_ID_PREFIXES : LIMITED_QUIRKS_WITH_SYSTEM_ID_PUBLIC_ID_PREFIXES;

                if (hasPrefix(publicId, prefixes))
                    return DOCUMENT_MODE.LIMITED_QUIRKS;
            }

            return DOCUMENT_MODE.NO_QUIRKS;
        }

        public static string serializeContent (string name, string publicId, string systemId)
        {
            string str = "!DOCTYPE";

            if (name != null)
                str += name;

            if (publicId != null)
                str += " PUBLIC " + enquoteDoctypeId(publicId);

            else if (systemId != null)
                str += " SYSTEM";

            if (systemId != null)
                str += " " + enquoteDoctypeId(systemId);

            return str;
        }
    }
}
