﻿using System;
using System.Globalization;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Ploeh.AutoFixture;
using Server.BusinessInterfaces.FieldDataPlugInCore.DataModel;
using Server.BusinessInterfaces.FieldDataPlugInCore.DataModel.DischargeActivities;
using Server.BusinessInterfaces.FieldDataPlugInCore.DataModel.DischargeSubActivities;
using Server.Plugins.FieldVisit.PocketGauger.Dtos;
using Server.Plugins.FieldVisit.PocketGauger.Helpers;
using Server.Plugins.FieldVisit.PocketGauger.Interfaces;
using Server.Plugins.FieldVisit.PocketGauger.Mappers;
using Server.Plugins.FieldVisit.PocketGauger.UnitTests.TestData;

namespace Server.Plugins.FieldVisit.PocketGauger.UnitTests.Mappers
{
    [TestFixture]
    public class DischargeActivityMapperTests
    {
        private IFixture _fixture;
        private IPointVelocityMapper _mockPointVelocityMapper;
        private GaugingSummaryItem _gaugingSummaryItem;

        private IDischargeActivityMapper _dischargeActivityMapper;

        private static TimeSpan LocationUtcOffset { get; } = TimeSpan.FromHours(3);

        [TestFixtureSetUp]
        public void SetupForAllTests()
        {
            _fixture = new Fixture();
            _fixture.Customizations.Add(new ProxyTypeSpecimenBuilder());
            CollectionRegistrar.Register(_fixture);

            SetupMockPointVelocityMapper();

            _dischargeActivityMapper = new DischargeActivityMapper(_mockPointVelocityMapper);
        }

        [SetUp]
        public void SetupForEachTest()
        {
            var startDate = _fixture.Create<DateTime>();
            var duration = _fixture.Create<TimeSpan>().Duration();

            _gaugingSummaryItem = _fixture.Build<GaugingSummaryItem>()
                .With(x => x.StartDate, startDate)
                .With(x => x.EndDate, startDate.Add(duration))
                .Create();
        }

        private void SetupMockPointVelocityMapper()
        {
            _mockPointVelocityMapper = Substitute.For<IPointVelocityMapper>();
            _mockPointVelocityMapper.Map(null, null).ReturnsForAnyArgs(new PointVelocityDischarge());
        }

        [Test]
        public void Map_GaugingSummaryStartDate_IsMappedDateTimeOffsetWithLocationUtcOffset()
        {
            var dischargeActivity = _dischargeActivityMapper.Map(_gaugingSummaryItem, LocationUtcOffset);

            AssertDateTimeOffsetIsNotDefault(dischargeActivity.MeasurementPeriod.Start);
        }

        private static void AssertDateTimeOffsetIsNotDefault(DateTimeOffset dateTimeOffset)
        {
            Assert.That(dateTimeOffset.Offset, Is.EqualTo(LocationUtcOffset));
            Assert.That(dateTimeOffset, Is.Not.EqualTo(default(DateTimeOffset)));
        }

        [Test]
        public void Map_GaugingSummaryEndDate_IsMappedDateTimeOffsetWithLocationUtcOffset()
        {
            var dischargeActivity = _dischargeActivityMapper.Map(_gaugingSummaryItem, LocationUtcOffset);

            AssertDateTimeOffsetIsNotDefault(dischargeActivity.MeasurementPeriod.End);
        }

        [Test]
        public void Map_GaugingSummaryFlowCalculationMethodIsMean_SetMonitoringMethodAsMeanSection()
        {
            _gaugingSummaryItem.FlowCalculationMethodProxy = FlowCalculationMethod.Mean.ToString();

            var dischargeActivity = _dischargeActivityMapper.Map(_gaugingSummaryItem, LocationUtcOffset);

            Assert.That(dischargeActivity.DischargeMethodCode, Is.EqualTo(ParametersAndMethodsConstants.MeanSectionMonitoringMethod));
        }

        [Test]
        public void Map_GaugingSummaryFlowCalculationMethodIsMid_SetMonitoringMethodAsMidSection()
        {
            _gaugingSummaryItem.FlowCalculationMethodProxy = FlowCalculationMethod.Mid.ToString();

            var dischargeActivity = _dischargeActivityMapper.Map(_gaugingSummaryItem, LocationUtcOffset);

            Assert.That(dischargeActivity.DischargeMethodCode, Is.EqualTo(ParametersAndMethodsConstants.MidSectionMonitoringMethod));
        }

        [Test]
        public void Map_GaugingSummaryFlowCalculationMethodIsUnknown_SetMonitoringMethodToDefault()
        {
            _gaugingSummaryItem.FlowCalculationMethodProxy = _fixture.Create<string>();

            var dischargeActivity = _dischargeActivityMapper.Map(_gaugingSummaryItem, LocationUtcOffset);

            Assert.That(dischargeActivity.DischargeMethodCode, Is.EqualTo(ParametersAndMethodsConstants.DefaultMonitoringMethod));
        }

        [Test]
        public void Map_GaugingSummaryUseIndexVelocityFlagSet_SetsMeanIndexVelocity()
        {
            _gaugingSummaryItem.UseIndexVelocityProxy = bool.TrueString;

            var dischargeActivity = _dischargeActivityMapper.Map(_gaugingSummaryItem, LocationUtcOffset);

            Assert.That(dischargeActivity.MeanIndexVelocity.Value, Is.EqualTo(_gaugingSummaryItem.IndexVelocity));
        }

        [Test]
        public void Map_GaugingSummaryUseIndexVelocityFlagNotSet_SetsMeanIndexVelocityToNull()
        {
            _gaugingSummaryItem.UseIndexVelocityProxy = bool.FalseString;

            var dischargeActivity = _dischargeActivityMapper.Map(_gaugingSummaryItem, LocationUtcOffset);

            Assert.That(dischargeActivity.MeanIndexVelocity, Is.EqualTo(null));
        }

        [Test]
        public void Map_GaugingSummaryWithStartAndEndStage_ProvidesBothStartAndEndStageMeasurements()
        {
            var startStage = _fixture.Create<double>();
            var endStage = _fixture.Create<double>();
            var meanStage = (startStage + endStage)/2D;

            _gaugingSummaryItem.StartStageProxy = startStage.ToString(CultureInfo.InvariantCulture);
            _gaugingSummaryItem.EndStageProxy = endStage.ToString(CultureInfo.InvariantCulture);
            _gaugingSummaryItem.MeanStageProxy = meanStage.ToString(CultureInfo.InvariantCulture);

            var dischargeActivity = _dischargeActivityMapper.Map(_gaugingSummaryItem, LocationUtcOffset);

            var expectedGaugeHeightMeasurements = new[]
            {
                new GageHeightMeasurement
                {
                    GageHeight = new Measurement(startStage, "m"),
                    MeasurementTime = dischargeActivity.MeasurementStartTime
                },
                new GageHeightMeasurement
                {
                    GageHeight = new Measurement(endStage, "m"),
                    MeasurementTime = dischargeActivity.MeasurementEndTime
                }
            };

            dischargeActivity.GageHeightMeasurements.ShouldAllBeEquivalentTo(expectedGaugeHeightMeasurements);
        }

        [Test]
        public void Map_GaugingSummaryWithOnlyMeanStage_ProvidesSingleMeanStageMeasurement()
        {
            var meanStage = _fixture.Create<double>();

            _gaugingSummaryItem.StartStageProxy = null;
            _gaugingSummaryItem.EndStageProxy = null;
            _gaugingSummaryItem.MeanStageProxy = meanStage.ToString(CultureInfo.InvariantCulture);

            var dischargeActivity = _dischargeActivityMapper.Map(_gaugingSummaryItem, LocationUtcOffset);

            var expectedMeasurementTime = new DateTimeOffset(
                (_gaugingSummaryItem.StartDate.Ticks + _gaugingSummaryItem.EndDate.Ticks)/2,
                LocationUtcOffset);

            var expectedGaugeHeightMeasurements = new[]
            {
                new GageHeightMeasurement
                {
                    GageHeight = new Measurement(meanStage, "m"),
                    MeasurementTime = expectedMeasurementTime
                }
            };

            dischargeActivity.GageHeightMeasurements.ShouldAllBeEquivalentTo(expectedGaugeHeightMeasurements);
        }

        [Test]
        public void Map_GaugingSummaryItem_IsMappedToExpectedDischargeActivity()
        {
            var expectedDischargeActivity = CreateExpectedDischargeActivity();

            var dischargeActivity = _dischargeActivityMapper.Map(_gaugingSummaryItem, LocationUtcOffset);

            dischargeActivity.ShouldBeEquivalentTo(expectedDischargeActivity, options => options
                .Excluding(activity => activity.MeasurementPeriod)
                .Excluding(activity => activity.MeanIndexVelocity)
                .Excluding(activity => activity.DischargeSubActivities)
                .Excluding(activity => activity.GageHeightMeasurements));
        }

        private DischargeActivity CreateExpectedDischargeActivity()
        {
            var startTime = new DateTimeOffset(_gaugingSummaryItem.StartDate, LocationUtcOffset);
            var endTime = new DateTimeOffset(_gaugingSummaryItem.EndDate, LocationUtcOffset);
            var surveyPeriod = new DateTimeInterval(startTime, endTime);
            var discharge = new Measurement(_gaugingSummaryItem.Flow.GetValueOrDefault(), ParametersAndMethodsConstants.DischargeUnitId);

            return new DischargeActivity(surveyPeriod, discharge)
            {
                Party = _gaugingSummaryItem.ObserversName,
                DischargeMethodCode = ParametersAndMethodsConstants.MidSectionMonitoringMethod,
                MeasurementId = _gaugingSummaryItem.GaugingId.ToString(NumberFormatInfo.InvariantInfo),
                ShowInDataCorrection = true,
                ShowInRatingDevelopment = true
            };
        }
    }
}
