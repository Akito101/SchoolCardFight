public static class PlayerArtifactChoice
{
    public static bool HasChoice { get; private set; }
    public static ArtifactEffect Effect { get; private set; } = ArtifactEffect.Protection;

    public static void Set(ArtifactEffect effect)
    {
        Effect = effect;
        HasChoice = true;
    }

    public static void Clear()
    {
        HasChoice = false;
    }
}