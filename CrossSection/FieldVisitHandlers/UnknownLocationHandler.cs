﻿using Server.BusinessInterfaces.FieldDataPluginCore.Context;
using Server.BusinessInterfaces.FieldDataPluginCore.DataModel.CrossSection;
using Server.BusinessInterfaces.FieldDataPluginCore.Results;

namespace Server.Plugins.FieldVisit.CrossSection.FieldVisitHandlers
{
    public class UnknownLocationHandler : FieldVisitHandlerBase
    {
         public UnknownLocationHandler(IFieldDataResultsAppender fieldDataResultsAppender)
            : base(fieldDataResultsAppender)
        {
        }

        public override FieldVisitInfo GetFieldVisit(string locationIdentifier, CrossSectionSurvey crossSectionSurvey)
        {
            var location = FieldDataResultsAppender.GetLocationByIdentifier(locationIdentifier);
            return CreateFieldVisit(location, crossSectionSurvey);
        }
    }
}
