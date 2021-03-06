﻿using FieldDataPluginFramework.DataModel;
using FieldDataPluginFramework.DataModel.ChannelMeasurements;
using FieldDataPluginFramework.DataModel.DischargeActivities;
using FieldDataPluginFramework.DataModel.Verticals;
using FieldDataPluginFramework.Units;
using StageDischargeReadings.Parsers;

namespace StageDischargeReadings.Mappers
{
    internal class DischargeActivityMapper
    {
        public DischargeActivity FromStageDischargeRecord(StageDischargeReadingRecord record)
        {
            var discharge = new Measurement(record.Discharge.GetValueOrDefault(), record.DischargeUnits);
            var dischargeInterval = new DateTimeInterval(record.MeasurementStartDateTime, record.MeasurementEndDateTime);
            var activity = new DischargeActivity(dischargeInterval, discharge)
            {
                MeasurementId = record.MeasurementId,
                Comments = record.Comments,
                Party = record.Party
            };

            AddGageHeightMeasurements(activity, record);

            var manualGaugingDischarge = CreateChannelMeasurementFromRecord(record, dischargeInterval, discharge);

            if (manualGaugingDischarge != null)
            {
                activity.ChannelMeasurements.Add(manualGaugingDischarge);
            }

            return activity;
        }

        private void AddGageHeightMeasurements(DischargeActivity activity, StageDischargeReadingRecord record)
        {
            if (record.StageAtStart.HasValue)
                activity.GageHeightMeasurements.Add(new GageHeightMeasurement(new Measurement(record.StageAtStart.GetValueOrDefault(), record.StageUnits), record.MeasurementStartDateTime));

            if (record.StageAtEnd.HasValue)
                activity.GageHeightMeasurements.Add(new GageHeightMeasurement(new Measurement(record.StageAtEnd.GetValueOrDefault(), record.StageUnits), record.MeasurementEndDateTime));
        }

        private static ChannelMeasurementBase CreateChannelMeasurementFromRecord(StageDischargeReadingRecord record, DateTimeInterval dischargeInterval, Measurement discharge)
        {
            var section = new ManualGaugingDischargeSectionFactory(CreateUnitSystem(record))
                { DefaultChannelName = record.ChannelName }
                .CreateManualGaugingDischargeSection(dischargeInterval, discharge.Value);

            section.AreaValue = record.ChannelArea;
            section.AreaUnitId = record.AreaUnits;
            section.WidthValue = record.ChannelWidth;
            section.DistanceUnitId = record.WidthUnits;
            section.VelocityAverageValue = record.ChannelVelocity;
            section.VelocityUnitId = record.VelocityUnits;
            section.ChannelName = record.ChannelName;
            section.Party = record.Party;

            section.DischargeMethod = DischargeMethodType.MidSection;
            section.VelocityObservationMethod = PointVelocityObservationType.Unknown;
            section.DeploymentMethod = DeploymentMethodType.Unspecified;
            section.MeterSuspension = MeterSuspensionType.Unspecified;

            return section;
        }

        private static UnitSystem CreateUnitSystem(StageDischargeReadingRecord record)
        {
            return new UnitSystem
            {
                DistanceUnitId = record.WidthUnits,
                AreaUnitId = record.AreaUnits,
                DischargeUnitId = record.DischargeUnits,
                VelocityUnitId = record.VelocityUnits
            };
        }
    }
}
