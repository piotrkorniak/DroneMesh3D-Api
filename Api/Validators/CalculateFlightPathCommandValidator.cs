using DroneMesh3D.Api.Commands;
using DroneMesh3D.Core.FlightPath;
using FluentValidation;

namespace DroneMesh3D.Api.Validators;

public sealed class CalculateFlightPathCommandValidator : AbstractValidator<CalculateFlightPathCommand>
{
    public CalculateFlightPathCommandValidator()
    {
        RuleFor(x => x.AreaId)
            .NotEqual(Guid.Empty)
            .WithMessage("AreaId must not be empty.");

        RuleFor(x => x.Mode)
            .IsInEnum()
            .WithMessage("Mode must be a valid FlightMode (Grid or Poi).");

        When(x => x.Mode == FlightMode.Grid, () =>
        {
            RuleFor(x => x.Grid)
                .NotNull()
                .WithMessage("Grid parameters are required when Mode is Grid.");

            When(x => x.Grid is not null, () =>
            {
                RuleFor(x => x.Grid!.AltitudeM)
                    .GreaterThan(0)
                    .WithMessage("Grid altitude must be greater than 0 m.")
                    .LessThanOrEqualTo(120)
                    .WithMessage("Grid altitude must not exceed 120 m.");

                RuleFor(x => x.Grid!.Camera.SensorWidthMm)
                    .GreaterThan(0)
                    .WithMessage("Camera sensor width must be greater than 0 mm.");

                RuleFor(x => x.Grid!.Camera.FocalLengthMm)
                    .GreaterThan(0)
                    .WithMessage("Camera focal length must be greater than 0 mm.");

                RuleFor(x => x.Grid!.Camera.ImageWidthPx)
                    .GreaterThan(0)
                    .WithMessage("Camera image width must be greater than 0 px.");

                RuleFor(x => x.Grid!.Camera.ImageHeightPx)
                    .GreaterThan(0)
                    .WithMessage("Camera image height must be greater than 0 px.");

                RuleFor(x => x.Grid!.FrontOverlapPercent)
                    .GreaterThanOrEqualTo(75)
                    .WithMessage("Front overlap must be at least 75%.")
                    .LessThanOrEqualTo(80)
                    .WithMessage("Front overlap must not exceed 80%.");

                RuleFor(x => x.Grid!.SideOverlapPercent)
                    .GreaterThanOrEqualTo(65)
                    .WithMessage("Side overlap must be at least 65%.")
                    .LessThanOrEqualTo(75)
                    .WithMessage("Side overlap must not exceed 75%.");
            });
        });

        When(x => x.Mode == FlightMode.Poi, () =>
        {
            RuleFor(x => x.Poi)
                .NotNull()
                .WithMessage("POI parameters are required when Mode is Poi.");

            When(x => x.Poi is not null, () =>
            {
                RuleFor(x => x.Poi!.AltitudeM)
                    .GreaterThan(0)
                    .WithMessage("POI altitude must be greater than 0 m.")
                    .LessThanOrEqualTo(120)
                    .WithMessage("POI altitude must not exceed 120 m.");

                RuleFor(x => x.Poi!.RadiusM)
                    .GreaterThan(0)
                    .WithMessage("POI radius must be greater than 0 m.");

                RuleFor(x => x.Poi!.GimbalPitchDegrees)
                    .GreaterThanOrEqualTo(-90)
                    .WithMessage("Gimbal pitch must be at least -90°.")
                    .LessThanOrEqualTo(-45)
                    .WithMessage("Gimbal pitch must not exceed -45°.");

                RuleFor(x => x.Poi!.CenterLatitude)
                    .GreaterThanOrEqualTo(-90)
                    .WithMessage("Center latitude must be at least -90°.")
                    .LessThanOrEqualTo(90)
                    .WithMessage("Center latitude must not exceed 90°.");

                RuleFor(x => x.Poi!.CenterLongitude)
                    .GreaterThanOrEqualTo(-180)
                    .WithMessage("Center longitude must be at least -180°.")
                    .LessThanOrEqualTo(180)
                    .WithMessage("Center longitude must not exceed 180°.");
            });
        });
    }
}
