using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace AK.Wwise.Unity.WwiseAddressables
{
    public class LoadBankAsyncOperation : AsyncOperationBase<WwiseSoundBankAsset>
    {
        private WwiseAddressableSoundBank _soundBank;

        private bool _decodeBank;
        private bool _saveDecodedBank;
        private bool _addToBankDictionary;

        private readonly AkAddressableBankManager _manager;
        
        public LoadBankAsyncOperation(WwiseAddressableSoundBank soundBank, AkAddressableBankManager manager, bool decodeBank = false, bool saveDecodedBank = false, bool addToBankDictionary = true)
        {
            _soundBank = soundBank;
            
            _decodeBank = decodeBank;
            _saveDecodedBank = saveDecodedBank;
            _addToBankDictionary = addToBankDictionary;

            _manager = manager;
        }
        
        protected override void Execute()
        {
            var addressableBanks = _manager.AddressableSoundBanks;
            
            _soundBank.decodeBank = _decodeBank;
            _soundBank.saveDecodedBank = _saveDecodedBank;
            if (addressableBanks.ContainsKey(_soundBank.name))
            {
                addressableBanks.TryGetValue(_soundBank.name, out _soundBank);
            }
            else if (_addToBankDictionary)
            {
                addressableBanks.TryAdd(_soundBank.name, _soundBank);
            }

            if (_soundBank.loadState == BankLoadState.Unloaded || _soundBank.loadState == BankLoadState.WaitingForInitBankToLoad)
            {
                if (!_manager.InitBankLoaded && _soundBank.name != "Init")
                {
                    _soundBank.loadState = BankLoadState.WaitingForInitBankToLoad;
                    Complete(null, false, null);
                    return;
                }
            }
            if (_soundBank.loadState == BankLoadState.Loading)
            {
                _soundBank.refCount += 1;
                Complete(null, false, null);
                return;
            }
            if (_soundBank.loadState == BankLoadState.Loaded)
            {
                _soundBank.refCount += 1;
                Complete(null, false, null);
                return;
            }

            _soundBank.refCount += 1;
            _soundBank.loadState = BankLoadState.Loading;
            
            AssetReferenceWwiseBankData bankData;
            if (_soundBank.Data.ContainsKey("SFX"))
            {
                UnityEngine.Debug.Log($"Wwise Addressable Bank Manager: Loading {_soundBank.name} bank");
                bankData = _soundBank.Data["SFX"];
                _soundBank.currentLanguage = "SFX";
            }
            else
            {
                var currentLanguage = AkSoundEngine.GetCurrentLanguage();
                if (_soundBank.Data.ContainsKey(currentLanguage))
                {
                    bankData = _soundBank.Data[currentLanguage];
                    _soundBank.currentLanguage = currentLanguage;
                    UnityEngine.Debug.Log($"Wwise Addressable Bank Manager: Loading {_soundBank.name} - {currentLanguage}");
                }
                else
                {
                    UnityEngine.Debug.LogError($"Wwise Addressable Bank Manager: {_soundBank.name} could not be loaded in {currentLanguage} language ");
                    addressableBanks.TryRemove(_soundBank.name, out _);
                    Complete(null, false, null);
                    return;
                }
            }
            
            LoadBankAsync(bankData);
        }

        private async void LoadBankAsync(AssetReferenceWwiseBankData bankData)
        {
            var asyncHandle = bankData.LoadAssetAsync();
            await asyncHandle.Task;
            if (asyncHandle.Status == AsyncOperationStatus.Succeeded)
            {
                _soundBank.eventNames = new HashSet<string>(asyncHandle.Result.eventNames);
                var data = asyncHandle.Result.RawData;
                _soundBank.GCHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
                var result = AkSoundEngine.LoadBankMemoryCopy(_soundBank.GCHandle.AddrOfPinnedObject(), (uint)data.Length, out var bankID);
                if (result == AKRESULT.AK_Success)
                {
                    _soundBank.soundbankId = bankID;
                }
                else
                {
                    _soundBank.soundbankId = AkAddressableBankManager.INVALID_SOUND_BANK_ID;
                }
                _soundBank.GCHandle.Free();
            }

            if (_soundBank.StreamingMedia != null)
            {
                foreach (var language in _soundBank.StreamingMedia.Keys)
                {
                    string destinationDir;
                    if (language == "SFX")
                    {
                        destinationDir = UnityEngine.Application.persistentDataPath; ;
                    }
                    else
                    {
                        destinationDir = Path.Combine(UnityEngine.Application.persistentDataPath, language);
                    }

                    if (!Directory.Exists(destinationDir))
                    {
                        Directory.CreateDirectory(destinationDir);
                    }

                    foreach (var streamedAsset in _soundBank.StreamingMedia[language].media)
                    {
                        if (streamedAsset == null)
                        {
                            UnityEngine.Debug.LogError($"Wwise Addressable Bank Manager: Streaming media asset referenced in {_soundBank.name} soundbank is null");
                            continue;
                        }
                        var handle = streamedAsset.LoadAssetAsync();
                        await handle.Task;

                        AkAssetUtilities.UpdateWwiseFileIfNecessary(destinationDir, handle.Result);
                        Addressables.Release(handle);
                    }
                }
            }

            //Make sure the asset is cleared from memory
            UnityEngine.Resources.UnloadUnusedAssets();

            _manager.OnBankLoaded(_soundBank);
            
            Complete(asyncHandle.Result, true, null);
            Addressables.Release(asyncHandle);
        }
    }
}
