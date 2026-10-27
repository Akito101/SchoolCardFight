using UnityEngine;

[CreateAssetMenu(fileName = "MusicLibrary", menuName = "Audio/Music Library")]
public class MusicLibrary : ScriptableObject
{
    [System.Serializable]
    public class Track
    {
        public string name;                 // just a label for you
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        public string[] scenes;             // names of the scenes that play this track
    }

    public Track[] tracks;
    public float fadeSeconds = 1f;

    public Track Find(string sceneName)
    {
        foreach (Track t in tracks)
        {
            foreach (string s in t.scenes)
            {
                if (s == sceneName) return t;
            }
        }
        return null;
    }
}