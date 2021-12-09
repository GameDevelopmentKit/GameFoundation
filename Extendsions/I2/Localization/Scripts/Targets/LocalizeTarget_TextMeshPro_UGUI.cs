#if TextMeshPro
namespace I2.Loc
{
    using System;
    using TMPro;
    using UnityEditor;
    using UnityEngine;

#if UNITY_EDITOR
    [InitializeOnLoad] 
    #endif

    public class LocalizeTarget_TextMeshPro_UGUI : LocalizeTarget<TextMeshProUGUI>
    {
        static LocalizeTarget_TextMeshPro_UGUI() { AutoRegister(); }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoRegister()
        {
            LocalizationManager.RegisterTarget(new LocalizeTargetDesc_Type<TextMeshProUGUI, LocalizeTarget_TextMeshPro_UGUI> { Name = "TextMeshPro UGUI", Priority = 100 });
        }

        public TextAlignmentOptions mAlignment_RTL = TextAlignmentOptions.Right;
        public TextAlignmentOptions mAlignment_LTR = TextAlignmentOptions.Left;
        public bool                 mAlignmentWasRTL;
        public bool                 mInitializeAlignment = true;

        public override eTermType GetPrimaryTermType(Localize cmp) { return eTermType.Text; }
        public override eTermType GetSecondaryTermType(Localize cmp) { return eTermType.TextMeshPFont; }
        public override bool CanUseSecondaryTerm() { return true; }
        public override bool AllowMainTermToBeRTL() { return true; }
        public override bool AllowSecondTermToBeRTL() { return false; }

        public override void GetFinalTerms ( Localize cmp, string Main, string Secondary, out string primaryTerm, out string secondaryTerm)
        {
            primaryTerm   = mTarget ? mTarget.text : null;
            secondaryTerm = this.mTarget.font != null ? this.mTarget.font.name : string.Empty;
        }



        public override void DoLocalize(Localize cmp, string mainTranslation, string secondaryTranslation)
        {
            {
                //--[ Localize Font Object ]----------
                var newFont = cmp.GetSecondaryTranslatedObj<TMP_FontAsset>(ref mainTranslation, ref secondaryTranslation);

                if (newFont != null)
                {
                    LocalizeTarget_TextMeshPro_Label.SetFont(mTarget, newFont);
                }
                else
                {
                    //--[ Localize Font Material ]----------
                    Material newMat = cmp.GetSecondaryTranslatedObj<Material>(ref mainTranslation, ref secondaryTranslation);
                    if (newMat != null && mTarget.fontMaterial != newMat)
                    {
                        if (!newMat.name.StartsWith(mTarget.font.name, StringComparison.Ordinal))
                        {
                            newFont = LocalizeTarget_TextMeshPro_Label.GetTMPFontFromMaterial(cmp, secondaryTranslation.EndsWith(newMat.name, StringComparison.Ordinal) ? secondaryTranslation : newMat.name);
                            if (newFont != null)
                                LocalizeTarget_TextMeshPro_Label.SetFont(mTarget, newFont);
                        }
                        LocalizeTarget_TextMeshPro_Label.SetMaterial( mTarget, newMat );
                    }
                }
            }

            if (mInitializeAlignment)
            {
                mInitializeAlignment = false;
                mAlignmentWasRTL = LocalizationManager.IsRight2Left;
                LocalizeTarget_TextMeshPro_Label.InitAlignment_TMPro(mAlignmentWasRTL, mTarget.alignment, out mAlignment_LTR, out mAlignment_RTL);
            }
            else
            {
                TextAlignmentOptions alignRTL, alignLTR;
                LocalizeTarget_TextMeshPro_Label.InitAlignment_TMPro(mAlignmentWasRTL, mTarget.alignment, out alignLTR, out alignRTL);

                if (this.mAlignmentWasRTL && this.mAlignment_RTL != alignRTL ||
                    !this.mAlignmentWasRTL && this.mAlignment_LTR != alignLTR)
                {
                    mAlignment_LTR = alignLTR;
                    mAlignment_RTL = alignRTL;
                }
                mAlignmentWasRTL = LocalizationManager.IsRight2Left;
            }

            if (mainTranslation != null && mTarget.text != mainTranslation)
            {
                if (cmp.CorrectAlignmentForRTL) this.mTarget.alignment = LocalizationManager.IsRight2Left ? this.mAlignment_RTL : this.mAlignment_LTR;
                this.mTarget.isRightToLeftText = LocalizationManager.IsRight2Left;
                if (LocalizationManager.IsRight2Left) mainTranslation = I2Utils.ReverseText(mainTranslation);

                mTarget.text = mainTranslation;
            }
        }
    }
}
#endif