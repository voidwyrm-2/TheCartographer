namespace TheCartographer;

public class Drugs
{
#pragma warning disable IDE0044
    static bool scugIsCarto = General.scugIsCarto;
#pragma warning restore IDE0044

    public static void ApplyCartoDRUGSHooks()
    {
        On.Player.SwallowObject += Player_IngestPuffDrugs;
        On.Player.SwallowObject += Player_IngestWeed;
        On.Player.Update += Player_IsOnDrugs;
        On.Player.Stun += Player_Stun;
        On.RoomCamera.ApplyFade += RoomCamera_ApplyFade;
    }

    private static void RoomCamera_ApplyFade(On.RoomCamera.orig_ApplyFade orig, RoomCamera self)
    {
#pragma warning disable IDE0031
        Creature creature = self.followAbstractCreature != null ? self.followAbstractCreature.realizedCreature : null;
#pragma warning restore IDE0031
        if (creature != null && creature is Player && scugIsCarto)
            self.mushroomMode = 3;

        orig(self);
    }

    private static void Player_IsOnDrugs(On.Player.orig_Update orig, Player self, bool eu)
    {
        scugIsCarto = General.scugIsCarto;
        General.IsOnDrugs = self.mushroomEffect != 0f;
        orig(self, eu);
    }

    private static void Player_Stun(On.Player.orig_Stun orig, Player self, int st)
    {
        if (scugIsCarto && self.mushroomEffect > 0f)
        {
            self.stun = 0;
            orig.Invoke(self, st);
        }
        else
        {
            orig.Invoke(self, st);
        }
    }

    private static void Player_IngestPuffDrugs(On.Player.orig_SwallowObject orig, Player self, int grasp)
    {
        if (scugIsCarto && self != null)
        {
            if (self.objectInStomach?.type == AbstractPhysicalObject.AbstractObjectType.PuffBall && self.FoodInStomach != self.MaxFoodInStomach)
            {
                //self.objectInStomach.type = AbstractPhysicalObject.AbstractObjectType.Rock;
                self.objectInStomach = new AbstractConsumable(self.room.world, AbstractPhysicalObject.AbstractObjectType.Rock, null, self.abstractPhysicalObject.pos, self.room.game.GetNewID(), -1, -1, null);
                self.AddFood(1);
                self.mushroomCounter += 400;
            }
        }
        orig(self, grasp);
    }

    private static void Player_IngestWeed(On.Player.orig_SwallowObject orig, Player self, int grasp)
    {
        if (scugIsCarto && self != null)
        {
            if (self.objectInStomach?.type == AbstractPhysicalObject.AbstractObjectType.FlyLure && self.FoodInStomach != self.MaxFoodInStomach)
            {
                //self.objectInStomach.Destroy();
                self.objectInStomach = null;//new AbstractConsumable(self.room.world, null, null, self.abstractPhysicalObject.pos, self.room.game.GetNewID(), -1, -1, null);
                self.AddQuarterFood();
                self.mushroomCounter += 230;
                self.mushroomEffect = 2f;
            }
        }
        orig(self, grasp);
    }
}
