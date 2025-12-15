using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HeatmapModule;
using System.Threading.Tasks;

namespace Heatmaps
{
    public class Heatmaps : MonoBehaviour
    {
        [Header("Assets")]
        [SerializeField] private Image heatmapImage;
        [SerializeField] private Image lineImage;
        [SerializeField] private Image gazeplotImage;
        [SerializeField] private Toggle heatmapToggle;
        [SerializeField] private Toggle lineToggle;
        [SerializeField] private Toggle gazeplotToggle;
        [SerializeField] private AudioClip[] audioClips;
        [SerializeField] private GameObject heatmapsPanel;

        [Header("Parameter")]

        [Range(1f, 20f)]
        public float insensitivity = 10f;

        public int heatmapRadius = 50;
        public int gazeplotRadius = 30;

        private AudioSource audioSource;
        private readonly Heatmap heatmap = new();

        // Start is called before the first frame update
        void Start()
        {
            audioSource = GetComponent<AudioSource>();
            Init();
        }

        public void Init()
        {
            heatmapImage.sprite = null;
            lineImage.sprite = null;
            gazeplotImage.sprite = null;

            heatmapImage.gameObject.SetActive(false);
            lineImage.gameObject.SetActive(false);
            gazeplotImage.gameObject.SetActive(false);
            heatmapsPanel.SetActive(false);
        }

        public async Task SetHeatmaps(List<Vector2> coords, int[] resolutions, bool isHeatmapVisible, bool isLineVisible, bool isGazeplotVisible)
        {
            await Task.Run(() => {

                int[,] intCoords = CoordConverter(coords);
                byte[] heatmapBytes = heatmap.MakeHeatmap(intCoords, resolutions, heatmapRadius, insensitivity);
                byte[] lineBytes = heatmap.MakeLine(intCoords, resolutions);
                byte[] gazeplotBytes = heatmap.MakeGazeplot(intCoords, resolutions, gazeplotRadius);

                UnityMainThreadDispatcher.Enqueue(() =>
                {
                    Sprite heatmapSprite = ImageConverter.CreateSpriteFromBytes(heatmapBytes);
                    Sprite lineSprite = ImageConverter.CreateSpriteFromBytes(lineBytes);
                    Sprite gazeplotSprite = ImageConverter.CreateSpriteFromBytes(gazeplotBytes);

                    heatmapImage.sprite = heatmapSprite;
                    heatmapImage.gameObject.SetActive(isHeatmapVisible);
                    heatmapToggle.isOn = isHeatmapVisible;

                    lineImage.sprite = lineSprite;
                    lineImage.gameObject.SetActive(isLineVisible);
                    lineToggle.isOn = isLineVisible;

                    gazeplotImage.sprite = gazeplotSprite;
                    gazeplotImage.gameObject.SetActive(isGazeplotVisible);
                    gazeplotToggle.isOn = isGazeplotVisible;

                    heatmapsPanel.SetActive(true);
                });
            });
        }

        private int[,] CoordConverter(List<Vector2> coords)
        {
            Vector2[] _coords = coords.ToArray();
            int[,] intCoords = new int[_coords.Length, 2];

            for (int i = 0; i < _coords.Length; i++)
            {
                int[] vectors = { (int)_coords[i].x, (int)_coords[i].y };

                for (int j = 0; j < 2; j++)
                    intCoords[i, j] = vectors[j];
            }

            return intCoords;
        }

        public void OnToggleValueChanged(Toggle toggle)
        {
            if (toggle.isOn)
                audioSource.PlayOneShot(audioClips[1]);
            else
                audioSource.PlayOneShot(audioClips[0]);

            switch (toggle.name)
            {
                case "HeatmapToggle":

                    heatmapImage.gameObject.SetActive(toggle.isOn);
                    break;

                case "LineToggle":

                    lineImage.gameObject.SetActive(toggle.isOn);
                    break;

                case "GazeplotToggle":

                    gazeplotImage.gameObject.SetActive(toggle.isOn);
                    break;

                default:
                    break;
            }
        }
    }
}
