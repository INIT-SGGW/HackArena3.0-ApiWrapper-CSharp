using src.HackArena3.Enums;
using src.HackArena3.Models;
using ProtoRace = HA3.Proto.Race.V1;

namespace src.HackArena3.Runtime;

internal static class MessageBuilder
{
    public static ProtoRace.ParticipantClientMessage ControlsMessage(Controls controls)
    {
        return new ProtoRace.ParticipantClientMessage
        {
            Controls = new ProtoRace.ParticipantControlsInput
            {
                ClientSeq = 1,
                Throttle = controls.Throttle,
                Brake = controls.Brake,
                Steering = controls.Steering,
                GearShift = (ProtoRace.GearShift)controls.GearShift,
                BrakeBalancer = controls.BrakeBalancer,
                DifferentialLock = controls.DifferentialLock
            }
        };
    }

    public static ProtoRace.ParticipantClientMessage BackToTrack()
    {
        return new ProtoRace.ParticipantClientMessage
        {
            BackToTrack = new ProtoRace.ParticipantBackToTrackCommand
            {
                ClientSeq = 1,
            }
        };
    }

    public static ProtoRace.ParticipantClientMessage EmergencyPitstop()
    {
        return new ProtoRace.ParticipantClientMessage
        {
            EmergencyPitstop = new ProtoRace.ParticipantEmergencyPitstopCommand
            {
                ClientSeq = 1,
            }
        };
    }

    public static ProtoRace.ParticipantClientMessage SetNextPitTireTypeMessage(TireType tireType)
    {
        return new ProtoRace.ParticipantClientMessage
        {
            SetNextPitTireType = new ProtoRace.ParticipantSetNextPitTireTypeCommand { 
                ClientSeq= 1,
                NextTireType = (ProtoRace.TireType)tireType 
            }
        };
    }
}
