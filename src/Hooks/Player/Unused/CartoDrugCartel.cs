using UnityEngine;
using MoreSlugcats;
using RWCustom;
using System.Collections.Generic;
using Random = UnityEngine.Random;
using Color = UnityEngine.Color;
using System.Runtime.CompilerServices;
using System.Linq;

namespace TheCartographer;

public static class MushroomStorage
{
    public class CartoDrugCartel
    {
        // Define your variables to store here!

        public Stack<AbstractPhysicalObject> Storage;
        public int GraspToTakeFrom;
        public bool Stuffed;
        public bool Floaty;
        public int SwallowOrRegurgitateCoutner;

        public CartoDrugCartel()
        {
            // Initialize your variables here! (Anything not added here will be null or false or 0 (default values))
            this.Storage = new Stack<AbstractPhysicalObject>();
            this.Stuffed = false;
            this.Floaty = false;
            this.SwallowOrRegurgitateCoutner = 0;
        }
    }

    // This part lets you access the stored stuff by simply doing "self.GetCat()" in Plugin.cs or everywhere else!
    private static readonly ConditionalWeakTable<Player, CartoDrugCartel> CWT = new();
    public static CartoDrugCartel GetCat(this Player player) => CWT.GetValue(player, _ => new());
}
public static class StoreMechanicsCarto
{
    public static int capacity = 3;

    public static bool StoreConditions(Player self)
    {
        bool Storeinputs = self.input[0].y > 0 && (self.bodyChunks[1].contactPoint.y < 0 || self.animation == Player.AnimationIndex.StandOnBeam || self.animation == Player.AnimationIndex.BeamTip || (!self.submerged && self.Submersion > 0f)) && self.eatMeat <= 20 && !self.input[0].pckp && self.input[0].x == 0;
        //bool hands = (self.grasps[1] != null && self.grasps[0] == null) || (self.grasps[0] != null && self.grasps[1] == null);

        if (!Storeinputs) return false; // Incorrect inputs, returning false!

        if (self.room == null) return false;

        if (self.grasps[0] == null && self.grasps[1] == null) return false;

        int handnum;
        if (self.grasps[0] != null) handnum = 0; // make the variable match the index of the grasp
        else handnum = 1;

        PhysicalObject grabbed = self.grasps[handnum].grabbed;
        self.GetCat().GraspToTakeFrom = handnum; // remember the index of the hand we're stealing from

        if (grabbed is Creature) return (grabbed as Creature).dead;
        return true;
    }

    public static bool RetrieveConditions(Player self)
    {
        bool Storeinputs = self.input[0].thrw && self.eatMeat <= 20 && !self.input[0].pckp && !self.input[0].jmp && !self.input[0].AnyDirectionalInput && !self.submerged;

        return Storeinputs && self.room != null && self.FreeHand() != -1;
    }

    public static void UpdateSkills(Player self)
    {
        //update the world of the stored objects

        if (StoreConditions(self) && self.GetCat().Storage.Count < capacity)
        {
            self.GetCat().SwallowOrRegurgitateCoutner++;
            if (self.GetCat().SwallowOrRegurgitateCoutner > 90)
            {
                SwallowIntoStorage(self, self.GetCat().GraspToTakeFrom);
                self.GetCat().SwallowOrRegurgitateCoutner = 0;
                (self.graphicsModule as PlayerGraphics).swallowing = 20;
            }
        }
        else if (RetrieveConditions(self) && self.GetCat().Storage.Count > 0)
        {
            self.GetCat().SwallowOrRegurgitateCoutner++;
            if (self.GetCat().SwallowOrRegurgitateCoutner > 90)
            {
                SpitUpFromStorage(self);
                self.GetCat().SwallowOrRegurgitateCoutner = 0;
            }
        }
        else
        {
            self.GetCat().SwallowOrRegurgitateCoutner = 0;
        }
        self.GetCat().Stuffed = self.GetCat().Storage.Count >= 5;
        self.GetCat().Floaty = StorageContains(self, MoreSlugcatsEnums.AbstractObjectType.EnergyCell, false, null);
    }

    public static void UpdateRegurgitationGraphics(PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser)
    {
        if (self.player.GetCat().SwallowOrRegurgitateCoutner > 15)
        {
            self.blink = 5;
        }
        float num10 = Mathf.InverseLerp(0f, 110f, (float)self.player.GetCat().SwallowOrRegurgitateCoutner);
        float num11 = (float)self.player.GetCat().SwallowOrRegurgitateCoutner / Mathf.Lerp(30f, 15f, num10);
        if (self.player.standing)
        {
            sLeaser.sprites[0].y += Mathf.Sin(num11 * 3.1415927f * 2f) * num10 * 2f;
            sLeaser.sprites[1].y += -Mathf.Sin((num11 + 0.2f) * 3.1415927f * 2f) * num10 * 3f;

            sLeaser.sprites[3].y += Mathf.Sin(num11 * 3.1415927f * 2f) * num10 * 2f;

            sLeaser.sprites[9].y += Mathf.Sin(num11 * 3.1415927f * 2f) * num10 * 2f;
        }
        else
        {
            sLeaser.sprites[0].y += Mathf.Sin(num11 * 3.1415927f * 2f) * num10 * 3f;
            sLeaser.sprites[0].x += Mathf.Cos(num11 * 3.1415927f * 2f) * num10 * 1f;
            sLeaser.sprites[1].y += Mathf.Sin((num11 + 0.2f) * 3.1415927f * 2f) * num10 * 2f;
            sLeaser.sprites[1].x += -Mathf.Cos(num11 * 3.1415927f * 2f) * num10 * 3f;


            sLeaser.sprites[3].y += Mathf.Sin(num11 * 3.1415927f * 2f) * num10 * 3f;
            sLeaser.sprites[3].x += Mathf.Cos(num11 * 3.1415927f * 2f) * num10 * 1f;

            sLeaser.sprites[9].y += Mathf.Sin(num11 * 3.1415927f * 2f) * num10 * 3f;
            sLeaser.sprites[9].x += Mathf.Cos(num11 * 3.1415927f * 2f) * num10 * 1f;
        }
    }

    public static int NumOfHeatSourcesInStorage(Player self)
    {
        int howmany = 0;
        List<AbstractPhysicalObject> Storagearray = self.GetCat().Storage.ToList();
        for (int i = 0; i < Storagearray.Count; i++)
        {
            if (Storagearray[i] is not AbstractCreature && Storagearray[i].type == AbstractPhysicalObject.AbstractObjectType.Lantern)
            {
                howmany++;
            }
        }
        return howmany;
    }

    public static void SwallowIntoStorage(Player self, int grasp)
    {
        PhysicalObject abstractPhysicalObject = self.grasps[grasp].grabbed;
        if (abstractPhysicalObject is Spear)
        {
            (abstractPhysicalObject as Spear).abstractSpear.stuckInWallCycles = 0;
        }
        if (abstractPhysicalObject is ElectricSpear && (abstractPhysicalObject as ElectricSpear).abstractSpear.electricCharge > 0)
        {
            self.room.AddObject(new ZapCoil.ZapFlash(self.firstChunk.pos, 10f));
            self.room.PlaySound(SoundID.Zapper_Zap, self.firstChunk.pos, 1f, 1.5f + Random.value * 1.5f);
            if (self.Submersion > 0.5f)
            {
                self.room.AddObject(new UnderwaterShock(self.room, null, self.firstChunk.pos, 10, 800f, 2f, self, new Color(0.8f, 0.8f, 1f)));
            }
            self.Stun(200);
            self.room.AddObject(new CreatureSpasmer(self, false, 200));
            return;
        }
        if (abstractPhysicalObject is JellyFish)
        {
            self.room.PlaySound(SoundID.Centipede_Shock, self.firstChunk.pos);
            self.room.AddObject(new UnderwaterShock(self.room, self, self.firstChunk.pos, 10, 100f, 0f, null, new Color(0.8f, 0.8f, 1f)));
            self.room.AddObject(new CreatureSpasmer(self, false, 60));
            self.Stun(80);
            return;
        }

        var ToSwallow = abstractPhysicalObject.abstractPhysicalObject;
        self.GetCat().Storage.Push(ToSwallow);
        self.ReleaseGrasp(grasp);
        abstractPhysicalObject.abstractPhysicalObject.realizedObject.RemoveFromRoom();
        abstractPhysicalObject.abstractPhysicalObject.Abstractize(self.abstractCreature.pos);
        abstractPhysicalObject.abstractPhysicalObject.Room.RemoveEntity(self.GetCat().Storage.Peek());

        BodyChunk mainBodyChunk = self.mainBodyChunk;
        mainBodyChunk.vel.y += 2f;
        self.room.PlaySound(SoundID.Slugcat_Swallow_Item, self.mainBodyChunk);
    }

    public static void SpitUpFromStorage(Player self)
    {
        AbstractPhysicalObject stomach = self.GetCat().Storage.Peek();
        stomach.world = self.abstractCreature.world;

        if (stomach is AbstractCreature)
        {
            stomach = new AbstractCreature(self.room.world, (self.GetCat().Storage.Peek() as AbstractCreature).creatureTemplate, null, self.room.GetWorldCoordinate(self.firstChunk.pos), self.GetCat().Storage.Peek().ID);
        }

        self.room.abstractRoom.AddEntity(stomach);

        stomach.pos = self.abstractCreature.pos;
        stomach.RealizeInRoom();

        if (stomach.realizedObject is Creature)
        {
            (stomach.realizedObject as Creature).Die();
        }

        Vector2 vector = self.bodyChunks[0].pos;
        Vector2 a = Custom.DirVec(self.bodyChunks[1].pos, self.bodyChunks[0].pos);
        bool flag = false;
        if (Mathf.Abs(self.bodyChunks[0].pos.y - self.bodyChunks[1].pos.y) > Mathf.Abs(self.bodyChunks[0].pos.x - self.bodyChunks[1].pos.x) && self.bodyChunks[0].pos.y > self.bodyChunks[1].pos.y)
        {
            vector += Custom.DirVec(self.bodyChunks[1].pos, self.bodyChunks[0].pos) * 5f;
            a *= -1f;
            a.x += 0.4f * (float)self.flipDirection;
            a.Normalize();
            flag = true;
        }
        stomach.realizedObject.firstChunk.HardSetPosition(vector);
        stomach.realizedObject.firstChunk.vel = Vector2.ClampMagnitude((a * 2f + Custom.RNV() * Random.value) / stomach.realizedObject.firstChunk.mass, 6f);
        self.bodyChunks[0].pos -= a * 2f;
        self.bodyChunks[0].vel -= a * 2f;

        if (self.graphicsModule != null)
        {
            (self.graphicsModule as PlayerGraphics).head.vel += Custom.RNV() * Random.value * 3f;
        }
        for (int i = 0; i < 3; i++)
        {
            self.room.AddObject(new WaterDrip(vector + Custom.RNV() * Random.value * 1.5f, Custom.RNV() * 3f * Random.value + a * Mathf.Lerp(2f, 6f, Random.value), false));
        }
        self.room.PlaySound(SoundID.Slugcat_Regurgitate_Item, self.mainBodyChunk);

        if (flag && self.FreeHand() > -1)
        {
            self.SlugcatGrab(stomach.realizedObject, self.FreeHand());
        }
        self.GetCat().Storage.Pop();
    }

    public static bool StorageContains(Player self, AbstractPhysicalObject.AbstractObjectType objType, bool CheckForCreatures, CreatureTemplate.Type creatType)
    {
        List<AbstractPhysicalObject> Storagearray = self.GetCat().Storage.ToList();
        if (!CheckForCreatures)
        {
            for (int i = 0; i < Storagearray.Count; i++)
            {
                if (Storagearray[i].type == objType)
                {
                    return true;
                }
            }
            return false;
        }
        else
        {
            for (int i = 0; i < Storagearray.Count; i++)
            {
                if ((Storagearray[i] as AbstractCreature).creatureTemplate.type == creatType)
                {
                    return true;
                }
            }
            return false;
        }
    }

    /*public static void UpdateSkills(Player self)
    {
        if (HiddenStorage.StoreConditions(self))
        {
            self.GetCat().LoadCounter++;

            if (self.GetCat().LoadCounter > 40)
            {
                HiddenStorage.AddToStorage(self);
                self.GetCat().LoadCounter = 0;
            }
        }
        else
        {
            self.GetCat().LoadCounter = 0;
        }


        if (HiddenStorage.RetrieveConditions(self))
        {
            self.GetCat().UnloadCounter++;
            if (self.GetCat().UnloadCounter > 40)
            {
                HiddenStorage.RetrieveFromStorage(self);
                self.GetCat().UnloadCounter = 0;
            }
        }
        else
        {
            self.GetCat().UnloadCounter = 0;
        }

        if (HiddenStorage.StoreConditions(self) || HiddenStorage.RetrieveConditions(self))
        {
            self.Blink(5);
        }

    }
    */
}