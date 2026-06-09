using UnityEngine;

namespace InkEcho.Hoai.Mock
{
    // ScriptableObject chứa dữ liệu album giả để test RevealAlbumPanel mà không cần server.
    // Create via: Assets > Create > InkEcho > Mock Album Data
    [CreateAssetMenu(menuName = "InkEcho/Mock Album Data", fileName = "MockAlbumData")]
    public class MockAlbumData : ScriptableObject
    {
        [System.Serializable]
        public class MockDrawingEntry
        {
            public string drawerName;
            public Texture2D image; // có thể để null để test trường hợp thiếu tranh
        }

        [System.Serializable]
        public class MockAlbum
        {
            public string ownerName;
            [TextArea(1, 3)] public string originalPrompt;
            public MockDrawingEntry[] drawings;
            public string finalGuess;
            public string guesserName;
        }

        public MockAlbum[] albums;
    }
}
