using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace Zero.AnomalyPrototype.Flow
{
    public sealed class CheckpointStore
    {
        private readonly string path;
        private readonly string temporaryPath;
        private readonly string backupPath;

        public CheckpointStore(string directory)
        {
            path = Path.Combine(directory, "zero-checkpoint.json");
            temporaryPath = path + ".tmp";
            backupPath = path + ".bak";
        }

        public bool TryLoad(out CheckpointData checkpoint)
        {
            if (TryRead(path, out checkpoint)) return true;
            if (TryRead(backupPath, out checkpoint))
            {
                Debug.LogWarning("CHECKPOINT_BACKUP_RECOVERED");
                return true;
            }

            checkpoint = null;
            if (File.Exists(path) || File.Exists(backupPath))
                Debug.LogError("CHECKPOINT_LOAD_INVALID: primary and backup failed validation");
            return false;
        }

        public bool Save(CheckpointData checkpoint)
        {
            if (checkpoint == null || !checkpoint.IsValid()) return false;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                string json = JsonUtility.ToJson(checkpoint);
                using (var stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                {
                    writer.Write(json);
                    writer.Flush();
                    stream.Flush(true);
                }

                if (File.Exists(path))
                    File.Replace(temporaryPath, path, TryRead(path, out _) ? backupPath : null);
                else File.Move(temporaryPath, path);
                Debug.Log($"CHECKPOINT_SAVED: kind={checkpoint.Kind}");
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"CHECKPOINT_SAVE_FAILED: {exception.Message}");
                return false;
            }
        }

        public bool Clear()
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
                if (File.Exists(backupPath)) File.Delete(backupPath);
                if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
                Debug.Log("CHECKPOINT_CLEARED");
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"CHECKPOINT_CLEAR_FAILED: {exception.Message}");
                return false;
            }
        }

        private static bool TryRead(string filePath, out CheckpointData checkpoint)
        {
            checkpoint = null;
            if (!File.Exists(filePath)) return false;
            try
            {
                return CheckpointData.TryDeserialize(File.ReadAllText(filePath), out checkpoint);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"CHECKPOINT_FILE_INVALID: path={filePath}, reason={exception.Message}");
                checkpoint = null;
                return false;
            }
        }
    }
}
