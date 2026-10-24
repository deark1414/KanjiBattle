using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace KanjiBattle.Editor
{
    public static class PieceMotifImporter
    {
        [MenuItem("KanjiBattle/Art/Import Reviewed Piece Motifs")]
        public static void ImportReviewed()
        {
            CharacterArtImporter.ImportProduction();
        }
    }
}
