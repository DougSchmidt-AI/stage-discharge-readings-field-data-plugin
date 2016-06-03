﻿using Server.Plugins.FieldVisit.PocketGauger.Dtos;
using Server.Plugins.FieldVisit.PocketGauger.Interfaces;

namespace Server.Plugins.FieldVisit.PocketGauger
{
    public class GaugingSummaryParser : IGaugingSummaryParser
    {
        public GaugingSummary Parse(PocketGaugerFiles pocketGaugerFiles)
        {
            return pocketGaugerFiles.ParseType<GaugingSummary>();
        }
   }
}
