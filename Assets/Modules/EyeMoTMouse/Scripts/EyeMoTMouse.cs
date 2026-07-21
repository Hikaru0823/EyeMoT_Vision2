using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace EyeMoTMouseModule
{
    public class EyeMoTMouse : Singleton<EyeMoTMouse>
    {
        [Header("Assets")]
        [SerializeField] private Sprite[] icons = default;
        [SerializeField] private Button gazeButton = default;
        [SerializeField] private Image _gazeButtonImage = default;
        [SerializeField] private Image blurImage = default;

        private Image gazeButtonImage = default;
        private AudioSource audioSource = default;
        private Keyboard keyboard = default;
        private Process cmdProcess = default;

        private bool isTrackable = false;
        private bool isInitialized = false;

        void Start()
        {
            Process[] eyeMoTProceses = Process.GetProcessesByName("EyeMoTMouse");

            if (eyeMoTProceses.Length > 0)
            {
                foreach (Process eyeMoTProcess in eyeMoTProceses)

                    //�v���Z�X�������I�ɏI��������
                    eyeMoTProcess.Kill();
            }

            if (this.cmdProcess == null)
            {
                this.cmdProcess = new Process();

                #if UNITY_WEBGL
                    gazeButton.interactable = false;
                    return;
                #else
                    this.cmdProcess.StartInfo.FileName = Application.dataPath + "/../EyeMoTMouse/EyeMoTMouse.exe";

                    this.cmdProcess.StartInfo.Arguments = "30";

                    // this.cmdProcess.StartInfo.CreateNoWindow = true; 

                    this.cmdProcess.EnableRaisingEvents = true;

                    this.cmdProcess.Exited += CmdProcessExited;

                    this.cmdProcess.StartInfo.UseShellExecute = false;

                    this.cmdProcess.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;

                    this.cmdProcess.StartInfo.RedirectStandardOutput = true;

                    this.cmdProcess.StartInfo.RedirectStandardInput = true;

                    // �W���o�̓C�x���g�ݒ�.
                    this.cmdProcess.OutputDataReceived += OutputHandler;

                    this.cmdProcess.Start();

                    this.cmdProcess.BeginOutputReadLine();

                    this.gazeButtonImage = _gazeButtonImage;
                    this.audioSource = this.GetComponent<AudioSource>();
                    this.keyboard = Keyboard.current;

                    this.OnStatusChanged(!this.isTrackable);
                    this.isInitialized = true;
                    #endif
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (this.keyboard != null)
            {
                if (this.keyboard.xKey.wasReleasedThisFrame)
                    this.OnStatusChanged(this.isTrackable);
            }
        }

        // EyeMoTMouse�̃R�}���h���C���ɉ����������܂ꂽ�Ƃ��ɓ��삷��
        private void OutputHandler(object sender, DataReceivedEventArgs args)
        {
            if (!string.IsNullOrEmpty(args.Data))
            {
                string trimedArgs = args.Data.Trim(); // �擾���������񂩂�󔒕�������s������؂藎�Ƃ�����

                // 0: �I���@1: �I�t
                switch (trimedArgs)
                {
                    case "0":
                        return;

                    case "1":
                        return;

                    case "StartUp":
                        // this.cmdProcess.StandardInput.WriteLine("mouse_off");
                        return;
                }
            }
        }

        void CmdProcessExited(object sender, System.EventArgs e)
        {
            this.cmdProcess.Dispose();
            this.cmdProcess = null;
        }

        private void OnApplicationQuit()
        {
            #if UNITY_WEBGL
        #else
            if (this.cmdProcess != null)
            {
                this.cmdProcess.StandardInput.WriteLine("exit");

                //�v���Z�X���I������܂ōő�15�b�ҋ@����
                this.cmdProcess.WaitForExit(1500);
                this.cmdProcess.Kill();
                this.cmdProcess.Dispose();
                this.cmdProcess = null;
            }
        #endif
        }

        public void OnButtonClicked(Button button)
        {
            switch (button.name)
            {
                case "GazeButton":
                    SendState();
                    this.OnStatusChanged(this.isTrackable);
                    break;
            }
        }

        public void StatusChange(bool isTrackable)
        {
            this.OnStatusChanged(isTrackable);
        }

        public void SetBlurImageActive(bool isActive)
        {
            this.blurImage.gameObject.SetActive(isActive);
            gazeButton.interactable = !isActive;
        }

        public void SendState()
        {
            if(ClientManager.Instance == null) return;
            ClientManager.Instance.SendTcp(
            NetJson.ToJson(new NetMessage<StringPayload>
            {
                Type = NetMessageType.EyeMoTMouseStatus,
                SenderId = ClientManager.Instance.Idx,
                TargetId = -2, // client全員へ
                Payload = new StringPayload { Text = this.isTrackable.ToString() }
            })
        );
        }

        private void OnStatusChanged(bool isTrackable)
        {
            if (isTrackable)
            {
                this.gazeButtonImage.sprite = this.icons[0];
                this.cmdProcess.StandardInput.WriteLine("mouse_off");
                this.isTrackable = false;
            }
            else
            {
                this.gazeButtonImage.sprite = this.icons[1];
                this.cmdProcess.StandardInput.WriteLine("mouse_on");
                this.isTrackable = true;
            }

            // if (this.isInitialized)
            //     this.audioSource.Play();
        }
    }
}
