using UnityEngine;
using System.IO;

public class IntrinsicsConverter : MonoBehaviour
{
    [System.Serializable]
    public class OpenCVIntrinsics
    {
        public Matrix Intrinsics;
        public Matrix DistCoeffs;
    }

    [System.Serializable]
    public class Matrix
    {
        public string type_id;
        public int rows;
        public int cols;
        public string dt;
        public double[] data;
    }

    [System.Serializable]
    public class VuforiaIntrinsics
    {
        public double fx, fy, cx, cy;
        public double k1, k2, p1, p2, k3;
    }

    void Start()
    {
        string filePath = Application.streamingAssetsPath + "/intrinsics.json";
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            OpenCVIntrinsics openCVData = JsonUtility.FromJson<OpenCVIntrinsics>(json);

            // Convert to Vuforia format
            VuforiaIntrinsics vuforiaData = new VuforiaIntrinsics
            {
                fx = openCVData.Intrinsics.data[0],
                fy = openCVData.Intrinsics.data[4],
                cx = openCVData.Intrinsics.data[2],
                cy = openCVData.Intrinsics.data[5],
                k1 = openCVData.DistCoeffs.data[0],
                k2 = openCVData.DistCoeffs.data[1],
                p1 = openCVData.DistCoeffs.data[2],
                p2 = openCVData.DistCoeffs.data[3],
                k3 = openCVData.DistCoeffs.data[4]
            };

            // Save the converted file
            string convertedJson = JsonUtility.ToJson(vuforiaData, true);
            File.WriteAllText(Application.streamingAssetsPath + "/vuforia_intrinsics.json", convertedJson);
            Debug.Log("Converted Intrinsics saved to: " + filePath);
        }
        else
        {
            Debug.LogError("Intrinsics file not found!");
        }
    }
}
