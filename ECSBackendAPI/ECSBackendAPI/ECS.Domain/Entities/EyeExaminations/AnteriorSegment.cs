using ECS.Domain.Entities.General;

namespace ECS.Domain.Entities.EyeExaminations
{
    public class AnteriorSegment : EntityBase<Guid>
    {
        public Guid ExamId { get; set; }
        public string Side { get; set; } = null!;

        // Mi mắt
        public bool EyelidNormal { get; set; } = true;
        public bool EyelidEdema { get; set; } = false;
        public bool EyelidPtosis { get; set; } = false;
        public string? PtosisDegree { get; set; }
        public bool EyelidEntropion { get; set; } = false;
        public string? EntropionUpperLocation { get; set; }
        public string? EntropionLowerLocation { get; set; }
        public bool EyelidEctropion { get; set; } = false;
        public bool EyelidLagophthalmos { get; set; } = false;
        public bool EyelidColoboma { get; set; } = false;
        public string? ColobomaLocation { get; set; }
        public bool EyelidLaceration { get; set; } = false;
        public string? LacerationDepth { get; set; }
        public bool? LacerationSutured { get; set; }
        public bool EyelidScar { get; set; } = false;
        public bool EyelidChalazion { get; set; } = false;
        public bool EyelidHordeolum { get; set; } = false;
        public bool EyelidTumor { get; set; } = false;
        public string? TumorNature { get; set; }
        public string? TumorLocation { get; set; }
        public string? TumorSize { get; set; }
        public bool CanaliculusNormal { get; set; } = true;
        public bool CanaliculusLaceration { get; set; } = false;
        public string? CanaliculusLacerationLocation { get; set; }
        public bool MeibomianNormal { get; set; } = true;
        public string? MeibomianBlockageDegree { get; set; }
        public bool Blepharitis { get; set; } = false;
        public string? EyelidOtherNote { get; set; }

        // Kết mạc
        public bool ConjunctivaNormal { get; set; } = true;
        public string? ConjunctivaCongestionType { get; set; }
        public bool ConjunctivaEdema { get; set; } = false;
        public bool ConjunctivaHemorrhage { get; set; } = false;
        public bool ConjunctivaPapilla { get; set; } = false;
        public bool ConjunctivaFollicle { get; set; } = false;
        public bool ConjunctivaKeratinization { get; set; } = false;
        public bool ConjunctivaScar { get; set; } = false;
        public bool ConjunctivaDischargePurulent { get; set; } = false;
        public bool ConjunctivaDischargeClear { get; set; } = false;
        public bool ConjunctivaPseudomembrane { get; set; } = false;
        public bool ConjunctivaFluoresceinStain { get; set; } = false;
        public bool ConjunctivaLaceration { get; set; } = false;
        public bool ConjunctivaIschemia { get; set; } = false;
        public bool ConjunctivaTumor { get; set; } = false;
        public string? ConjunctivaTumorNature { get; set; }
        public string? ConjunctivaTumorLocation { get; set; }
        public string? ConjunctivaTumorSize { get; set; }
        public string? FornixStatus { get; set; }
        public string? FornixSymblepharonHeight { get; set; }
        public string? FornixSymblepharonWidth { get; set; }
        public string? ConjunctivaOtherNote { get; set; }

        // Giác mạc
        public string? CorneaClarity { get; set; }
        public string? CorneaSize { get; set; }
        public string? CorneaShape { get; set; }
        public decimal? CorneaDiameterMm { get; set; }
        public bool EpitheliumPunctateLesion { get; set; } = false;
        public string? EpitheliumBullousEdema { get; set; }
        public string? EpitheliumLossArea { get; set; }
        public string? EpitheliumLossLocation { get; set; }
        public string? EpitheliumLossEdge { get; set; }
        public bool BandKeratopathy { get; set; } = false;
        public bool DrugDeposit { get; set; } = false;
        public string? StromaEdema { get; set; }
        public string? StromaInfiltrateDepth { get; set; }
        public string? StromaInfiltrateDistribution { get; set; }
        public string? StromaThinning { get; set; }
        public bool CornealUlcer { get; set; } = false;
        public string? UlcerSize { get; set; }
        public string? UlcerLocation { get; set; }
        public string? UlcerEdge { get; set; }
        public string? EndotheliumFolds { get; set; }
        public bool PigmentDepositPosterior { get; set; } = false;
        public bool PusPosterior { get; set; } = false;
        public bool ExudatePosterior { get; set; } = false;
        public bool Guttata { get; set; } = false;
        public bool DescemetRupture { get; set; } = false;
        public bool DescemetScroll { get; set; } = false;
        public string? KeraticPrecipitates { get; set; }
        public bool CorneaPerforationThreatened { get; set; } = false;
        public bool CorneaIrisIncarceration { get; set; } = false;
        public bool CorneaPerforation { get; set; } = false;
        public string? PerforationLocation { get; set; }
        public bool PerforationSeidel { get; set; } = false;
        public decimal? PerforationDiameterMm { get; set; }
        public bool? PerforationSealed { get; set; }
        public bool CorneaLaceration { get; set; } = false;
        public string? LacerationSize { get; set; }
        public string? LacerationLocationCornea { get; set; }
        public string? LacerationType { get; set; }
        public bool? LacerationSuturedCornea { get; set; }
        public bool? LacerationSutureAnatomical { get; set; }
        public bool CorneaBloodStaining { get; set; } = false;
        public bool CorneaAbscess { get; set; } = false;
        public string? CorneaSensation { get; set; }
        public bool NeovascularizationSuperficial { get; set; } = false;
        public string? NeovascularizationSuperficialDirection { get; set; }
        public bool NeovascularizationDeep { get; set; } = false;
        public string? NeovascularizationExtent { get; set; }
        public bool LimbalStemCellDeficiency { get; set; } = false;
        public bool LimbalAgeDegeneration { get; set; } = false;
        public bool LimbalCalciumDeposit { get; set; } = false;
        public string? CorneaDvNote { get; set; }
        public string? CorneaOtherNote { get; set; }

        // Củng mạc
        public bool ScleraNormal { get; set; } = true;
        public bool ScleraEctasia { get; set; } = false;
        public bool ScleraThinning { get; set; } = false;
        public bool ScleraNecrosis { get; set; } = false;
        public bool ScleraEpiscleritis { get; set; } = false;
        public string? ScleraScleritisType { get; set; }
        public bool ScleraLaceration { get; set; } = false;
        public string? ScleraLacerationSize { get; set; }
        public string? ScleraLacerationLocation { get; set; }
        public bool? ScleraLacerationSutured { get; set; }
        public bool ScleraTissueIncarceration { get; set; } = false;
        public bool ScleraOldSurgeryScar { get; set; } = false;
        public string? ScleraSurgeryScarLocation { get; set; }
        public string? ScleraOtherNote { get; set; }

        // Tiền phòng
        public bool AcNormal { get; set; } = true;
        public decimal? AcDepthMm { get; set; }
        public string? AcDepthHerick { get; set; }
        public bool AcFlat { get; set; } = false;
        public bool AcLensMaterial { get; set; } = false;
        public decimal? AcPusMm { get; set; }
        public bool AcExudate { get; set; } = false;
        public string? AcTyndall { get; set; }
        public bool AcHemorrhage { get; set; } = false;
        public string? AcHemorrhageDegree { get; set; }
        public bool AcForeignBody { get; set; } = false;
        public decimal? AcBloodMm { get; set; }
        public bool AngleSynechiae { get; set; } = false;
        public bool AnglePigment { get; set; } = false;
        public bool AngleNeovascularization { get; set; } = false;
        public string? AngleOtherNote { get; set; }
        public string? AcOtherNote { get; set; }

        // Mống mắt
        public bool IrisNormal { get; set; } = true;
        public string? IrisColor { get; set; }
        public bool IrisDegeneration { get; set; } = false;
        public bool IrisNeovascularization { get; set; } = false;
        public bool IrisKoeppeNodules { get; set; } = false;
        public bool IrisBusaccaNodules { get; set; } = false;
        public bool IrisProlapse { get; set; } = false;
        public bool IrisIncarceration { get; set; } = false;
        public bool IrisRootTear { get; set; } = false;
        public string? IrisRootTearDegree { get; set; }
        public bool IrisLoss { get; set; } = false;
        public bool IrisPerforation { get; set; } = false;
        public bool IrisAtrophyOd { get; set; } = false;
        public decimal? IrisDiameterMm { get; set; }
        public string? IrisOtherNote { get; set; }

        // Đồng tử
        public bool PupilRound { get; set; } = true;
        public bool PupilIrregular { get; set; } = false;
        public decimal? PupilDiameterMm { get; set; }
        public bool PupilSynechiae { get; set; } = false;
        public string? PupilSynechiaeLocation { get; set; }
        public string? PupilReflex { get; set; }
        public bool PupilMydriasisParalysis { get; set; } = false;
        public string? PupilPigmentRuff { get; set; }
        public string? PupilLightReflex { get; set; }
        public string? FundusReflex { get; set; }
        public string? PupilOtherNote { get; set; }

        // Thể thủy tinh
        public bool LensClear { get; set; } = true;
        public string? LensOpacityType { get; set; }
        public bool LensRupture { get; set; } = false;
        public bool LensSubluxation { get; set; } = false;
        public bool LensIntoAc { get; set; } = false;
        public bool LensIntoVitreous { get; set; } = false;
        public bool LensEndophthalmitis { get; set; } = false;
        public bool LensForeignBody { get; set; } = false;
        public bool LensPigmentAdhesion { get; set; } = false;
        public bool IolPresent { get; set; } = false;
        public string? IolStatus { get; set; }
        public string? IolLocation { get; set; }
        public string? LensOtherNote { get; set; }

        // Dịch kính
        public bool VitreousClear { get; set; } = true;
        public bool VitreousOpacity { get; set; } = false;
        public bool VitreousEndophthalmitis { get; set; } = false;
        public bool VitreousHemorrhage { get; set; } = false;
        public bool VitreousOrganized { get; set; } = false;
        public bool VitreousPvd { get; set; } = false;
        public bool VitreousForeignBody { get; set; } = false;
        public string? VitreousTyndall { get; set; }
        public string? VitreousOtherNote { get; set; }

        public virtual EyeExamination Examination { get; set; } = null!;
    }
}
