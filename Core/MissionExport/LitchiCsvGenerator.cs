using System.Globalization;
using System.Text;
using DroneMesh3D.Core.FlightPath;

namespace DroneMesh3D.Core.MissionExport;

/// <summary>
///     Generates a Litchi-compatible CSV mission file from waypoints.
/// </summary>
public sealed class LitchiCsvGenerator : IMissionFileGenerator
{
    private const string Header =
        "latitude,longitude,altitude(m),heading(deg),curvesize(m),rotationdir,gimbalmode,gimbalpitchangle,actiontype1,actionparam1,speed(m/s)";

    public ExportFormat Format => ExportFormat.LitchiCsv;

    public MissionFileData Generate(Guid flightPlanId, IReadOnlyList<Waypoint> waypoints)
    {
        var sb = new StringBuilder(waypoints.Count * 110 + Header.Length + 10);
        sb.Append(Header);
        sb.Append('\n');

        foreach (var wp in waypoints)
        {
            sb.Append(wp.Latitude.ToString("F6", CultureInfo.InvariantCulture));
            sb.Append(',');
            sb.Append(wp.Longitude.ToString("F6", CultureInfo.InvariantCulture));
            sb.Append(',');
            sb.Append(wp.AltitudeAglM.ToString("F2", CultureInfo.InvariantCulture));
            sb.Append(',');
            sb.Append(wp.GimbalYawDegrees.ToString("F2", CultureInfo.InvariantCulture));
            sb.Append(',');
            sb.Append(MissionConstants.LitchiCurveSize.ToString(CultureInfo.InvariantCulture));
            sb.Append(',');
            sb.Append(MissionConstants.LitchiRotationDir.ToString(CultureInfo.InvariantCulture));
            sb.Append(',');
            sb.Append(MissionConstants.LitchiGimbalMode.ToString(CultureInfo.InvariantCulture));
            sb.Append(',');
            sb.Append(wp.GimbalPitchDegrees.ToString("F2", CultureInfo.InvariantCulture));
            sb.Append(',');
            sb.Append(MissionConstants.LitchiActionTypeTakePhoto.ToString(CultureInfo.InvariantCulture));
            sb.Append(',');
            sb.Append(MissionConstants.LitchiActionParam.ToString(CultureInfo.InvariantCulture));
            sb.Append(',');
            sb.Append(MissionConstants.MaxSpeedMs.ToString("F0", CultureInfo.InvariantCulture));
            sb.Append('\n');
        }

        var content = Encoding.UTF8.GetBytes(sb.ToString());

        return new MissionFileData(
            content,
            "text/csv",
            $"mission_{flightPlanId}.csv");
    }
}
