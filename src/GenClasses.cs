namespace TheCartographer
{
    public static class GenClasses
    {
        public class SpearStats
        {
            public float spearDamageBonus;
            public float gravity;
            public float bodyChunksVelY;

            public SpearStats(Spear spear)
            {
                spearDamageBonus = spear.spearDamageBonus;
                gravity = spear.gravity;
                bodyChunksVelY = spear.bodyChunks[0].vel.y;
            }

            public void ApplyStats(Spear spear)
            {
                spear.spearDamageBonus = spearDamageBonus;
                spear.gravity = gravity;
                spear.bodyChunks[0].vel.y = bodyChunksVelY;
            }
        }
    }
}
