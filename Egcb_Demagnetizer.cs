using System;
using System.Collections.Generic;
using XRL.Core;
using XRL.World.Parts.Mutation;

namespace XRL.World.Parts
{
    [Serializable]
    public class Egcb_Demagnetizer : IPart
    {
        private MagneticPulse MagnetMutation;
        private const int DefaultMutationLevel = 15;
        private Dictionary<string, bool> DocileZones = new Dictionary<string, bool>();

        public Egcb_Demagnetizer()
        {
            this.MagnetMutation = null;
        }

        public override void Register(GameObject Object)
        {
            Object.RegisterPartEvent(this, "EndTurn");
            Object.RegisterPartEvent(this, "ZoneActivated");
            Object.RegisterPartEvent(this, "ZoneDeactivated");
            Object.RegisterPartEvent(this, "ObjectCreated");
            base.Register(Object);
        }

        public override bool FireEvent(Event E)
        {
            if (E.ID == "ZoneActivated" || E.ID == "ObjectCreated")
            {
                this.MagnetMutation = this.ParentObject.GetPart<MagneticPulse>();
                if (this.MagnetMutation != null)
                {
                    this.Depolarize();
                }
            }
            else if (E.ID == "ZoneDeactivated")
            {
                if (this.MagnetMutation != null)
                {
                    this.Repolarize();
                }
                this.MagnetMutation = null;
            }
            else if (E.ID == "EndTurn")
            {
                if (this.MagnetMutation == null)
                {
                    this.MagnetMutation = this.ParentObject.GetPart<MagneticPulse>();
                }
                if (this.MagnetMutation != null)
                {
                    this.Depolarize();
                }
            }
            return base.FireEvent(E);
        }

        public void Depolarize()
        {
            if (this.IsDocileZone() && XRLCore.Core.Game.Player.Body.CurrentCell.ParentZone == this.ParentObject.CurrentCell.ParentZone)
            {
                if (!this.ParentObject.IsHostileTowards(XRLCore.Core.Game.Player.Body))
                {
                    this.MagnetMutation.BaseLevel = 1; //reduces the pulse range to only adjacent tiles.
                    if (this.ParentObject.HasTag("IsLibrarian"))
                    {
                        try
                        {
                            //additional mitigation if Sheba is a magnet, because we'd prefer she not even pulse at adjacent tiles.
                            this.MagnetMutation.ActivatedAbility.Cooldown = 200;
                        }
                        catch (Exception ex)
                        {
                            UnityEngine.Debug.Log("Demagnetizer Mod: Error trying to reset magnet mutation ability cooldown.");
                        }
                    }
                }
                else
                {
                    this.Repolarize(); //restore magnet power if the magnet is hostile to the player
                }
            }
        }

        public void Repolarize()
        {
            this.MagnetMutation.BaseLevel = Egcb_Demagnetizer.DefaultMutationLevel; //reset mutation level to normal
        }

        public bool IsDocileZone()
        {
            string zoneID = this.ParentObject.CurrentCell.ParentZone.ZoneID;
            if (!this.DocileZones.ContainsKey(zoneID))
            {
                foreach (ZoneBuilderBlueprint blueprint in XRLCore.Core.Game.ZoneManager.GetBuildersFor(this.ParentObject.CurrentCell.ParentZone))
                {
                    if (blueprint.Class == "Village" || blueprint.Class == "SixDayTents" || blueprint.Class == "VillageOutskirts"
                        || blueprint.Class == "VillageOver" || blueprint.Class == "VillageUnder" || blueprint.Class == "BeyLahOutskirts"
                        || blueprint.Class == "HindrenClues" || blueprint.Class == "JoppaOutskirts")
                    {
                        this.DocileZones.Add(zoneID, true);
                        break;
                    }
                }
                if (!this.DocileZones.ContainsKey(zoneID))
                {
                    this.DocileZones.Add(zoneID, false);
                }
            }
            return this.DocileZones[zoneID];
        }
    }
}
