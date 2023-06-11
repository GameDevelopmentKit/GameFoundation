/*! \cond PRIVATE */
using DarkTonic.MasterAudio;
using UnityEngine;

public static class ResonanceAudioHelper {
    public static bool ResonanceAudioOptionExists {
        get {
            return true;
        }
    }

    public static bool DarkTonicResonanceAudioPackageInstalled() {
        return false;
    }

    public static void AddResonanceAudioSourceToVariation(SoundGroupVariation variation) {
        return;
    }

    public static void AddResonanceAudioSourceToAllVariations() {
        return;
    }

    public static void RemoveResonanceAudioSourceFromAllVariations() {
        return;
    }

    public static void CopyResonanceAudioSource(DynamicGroupVariation sourceVariation, DynamicGroupVariation destVariation) {
        return;
    }

    public static void CopyResonanceAudioSource(DynamicGroupVariation sourceVariation, SoundGroupVariation destVariation) {
        return;
    }

    public static void CopyResonanceAudioSource(SoundGroupVariation sourceVariation, DynamicGroupVariation destVariation) {
        return;
    }
}
/*! \endcond */
