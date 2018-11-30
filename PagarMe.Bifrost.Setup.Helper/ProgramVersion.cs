﻿using System;
using System.IO;
using System.Text.RegularExpressions;

namespace PagarMe.Bifrost.Setup.Helper
{
    internal class ProgramVersion
    {
        protected const String MainDir = @"..\..\..\";

        protected static String GetCurrentVersion()
        {
            var assemblyInfoPath = Path.Combine(MainDir, "PagarMe.Generic", "Version.cs");
            var assemblyInfoContent = File.ReadAllText(assemblyInfoPath);

            var regexVersion = new Regex(@"(\d+.\d+.\d+)");
            var version = regexVersion.Match(assemblyInfoContent).Groups[1].Value;

            return version;
        }
    }
}