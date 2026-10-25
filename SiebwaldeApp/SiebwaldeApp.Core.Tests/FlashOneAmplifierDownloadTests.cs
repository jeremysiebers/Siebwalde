using System.Text;
using Xunit;

namespace SiebwaldeApp.Core.Tests;

/// <summary>
/// Byte-level regression tests for the firmware-file download used by the single-target
/// bootloader recovery path (<see cref="FlashOneAmplifierCommand"/>). These lock in the
/// exact 30720-byte PIC18 legacy image (checksum 0x251F, SHA-256 B848EF04...) and prove
/// the CLI sends it as exactly 480 ordered 64-byte packets whose PIC32-side 16-bit
/// little-endian word-sum equals 0x251F.
/// </summary>
public class FlashOneAmplifierDownloadTests
{
    /// <summary>
    /// Byte-exact 30720-byte application image (0x800..0x7FFF) of the legacy 0x251F
    /// PIC18 Offset firmware. The final 2 bytes are the linker-embedded checksum word
    /// 0x251F little-endian (0x1F, 0x25).
    /// </summary>
    private static readonly byte[] LegacyImage = HexToBytes(
            "87ef04f0ffffffff5882d9cf31f0dacf32f0f2ac0eef04f010ef04f081ef04f00e01c5ab16ef04f018ef04f056ef04f0" +
            "cdab1def04f01fef04f056ef04f09da224ef04f026ef04f047ef04f09d989d8847ef04f0000e9db0010e336e99cf34f0" +
            "33c02ef034509cec0cf084be3aef04f03def04f0010e3eef04f0000eff0a306e30323032845030187f0b3018846e0e01" +
            "cdbb4def04f04fef04f02aef04f03bec10f00e01ce95c68581ef04f0c6a55bef04f05def04f06def04f0cea562ef04f0" +
            "64ef04f06def04f0000e146e030e136ec695ce959d8681ef04f0c6a772ef04f074ef04f081ef04f0cea779ef04f07bef" +
            "04f081ef04f0000eb56ece972a2a81ef04f032c0daff31c0d9ff5892110062ef0df094ec0ef049ec0ff035c0c2f036c0" +
            "c3f0f28ef28c849a84968498849284948598859c858aaa0e576eaa0e1eec10f03def05f08db0a7ef04f0a9ef04f0abef" +
            "04f0010e196e84863def05f031ec0cf01b2aaaec05f08da0b8ef04f0baef04f0e3ef04f03f0e0001b015e76f000eb115" +
            "e86fe751e811d8b4c8ef04f0caef04f0d8ef04f0000e1b6e8496b0513f0b576eb18b57501eec10f0050e196ee3ef04f0" +
            "000e196e010e516e000e1b6e8486849a8498849284940001c251c311d8a4ebef04f0edef04f03def05f0020e196e3def" +
            "05f031ec0cf0020e4f185010d8a4fbef04f0fdef04f03def05f0030e196e3def05f031ec0cf04f045010d8a40aef05f0" +
            "0cef05f03def05f0ff003def05f031ec0cf01a2aaaec05f00001afaf1aef05f01cef05f03def05f0000e196e000e516e" +
            "000e1a6e8498848a3def05f01950000ad8b4a2ef04f0010ad8b4aeef04f0030ad8b4f1ef04f0010ad8b401ef05f0060a" +
            "d8b40fef05f03def05f05150d8a443ef05f045ef05f026ef05f033ec0ff031ec0cf0aaec05f02a50d8b451ef05f053ef" +
            "05f047ef05f0f7ec0ff00001e76fe73be7478350e719df0be719836e93ef05f059ec09f03b043c10d8a469ef05f06bef" +
            "05f0a6ef05f0010e186e000e2a6e47ef05f0e2ec0cf039043a10d8a47aef05f07cef05f0a6ef05f0020e186e000e2a6e" +
            "47ef05f0aeec07f039043a10d8a48bef05f08def05f0a6ef05f0000e186e000e2a6e47ef05f01850000ad8b460ef05f0" +
            "010ad8b471ef05f0030ad8b482ef05f0010ad8b48def05f08def05f047ef05f087ef04f00e01ceabb0ef05f0b2ef05f0" +
            "b7ef06f0ce9be3ef05f01a50d8b4bbef05f0bdef05f01cef06f084881ac020f0010e1c6e1cef06f020501a18d8b4cbef" +
            "05f0cdef05f0d6ef05f020501a18d8b4d4ef05f0d6ef05f01cef06f08498000e206e000e1a6e000e1c6e1cef06f0000e" +
            "1c6e1cef06f01c50000ad8b4b5ef05f0010ad8b4c4ef05f0dfef05f01b50d8b4f4ef05f0f6ef05f064ef06f084861bc0" +
            "21f0010e1d6e64ef06f021501b18d8b404ef06f006ef06f00fef06f021501b18d8b40def06f00fef06f064ef06f08496" +
            "000e216e000e1b6e000e1d6e64ef06f0000e1d6e64ef06f01d50000ad8b4eeef05f0010ad8b4fdef05f018ef06f00d50" +
            "0e10d8b42eef06f030ef06f0acef06f084820dc023f0010e1f6eacef06f02350356e366a0d50351809e10e503618d8b4" +
            "44ef06f046ef06f055ef06f02350356e366a0d50351809e10e503618d8b453ef06f055ef06f0acef06f08492000e236e" +
            "000e0e6e000e0d6e000e1f6eacef06f0000e1f6eacef06f01f50000ad8b427ef06f0010ad8b437ef06f060ef06f00b50" +
            "0c10d8b476ef06f078ef06f0b7ef06f084840bc022f0010e1e6eb7ef06f02250356e366a0b50351809e10c503618d8b4" +
            "8cef06f08eef06f09def06f02250356e366a0b50351809e10c503618d8b49bef06f09def06f0b7ef06f08494000e226e" +
            "000e0c6e000e0b6e000e1e6eb7ef06f0000e1e6eb7ef06f01e50000ad8b46fef06f0010ad8b47fef06f0a8ef06f01200" +
            "050e2960beef06f0c0ef06f0c3ef06f0020eadef07f0220e2964c9ef06f0cbef06f0cfef06f0000e296ec0ef06f000c1" +
            "40f001c141f052504018d8b4daef06f0dcef06f067ef07f028504018d8b4e3ef06f0e5ef06f067ef07f0000e296e010e" +
            "adef07f0000e476e080e466e8eef07f0070e2960f6ef06f0f8ef06f0faef06f0c0ef06f005c135f00101045116ec10f0" +
            "35c042f036c043f006c144f0456a42504310d8a40eef07f010ef07f012ef07f0cbef06f0100e4118d8a419ef07f01bef" +
            "07f02eef07f0d89042343c6e43343d6e3c50441809e13d504518d8b42aef07f02cef07f04fef07f0cbef06f0070e4224" +
            "3a6e000e43203b6ed8903b323a32d8903b323a32d8903b323a323ac03ef03bc03ff03e50441809e13f504518d8b44bef" +
            "07f04def07f04fef07f0cbef06f0090e4424466e000e4520476e475009e1230e465cd8a05eef07f060ef07f08eef07f0" +
            "cbef06f0000e296e000eadef07f04150010ad8b4eaef06f0030ad8b4eaef06f0010ad8b4eaef06f0070ad8b4eaef06f0" +
            "010ad8b4eaef06f0030ad8b4eaef06f00e0ad8b4eaef06f0070ad8b4f0ef06f01f0ad8b4f0ef06f062ef07f029503a6e" +
            "3b6a46503a1807e147503b18d8a49bef07f09def07f0a0ef07f0030eadef07f02950465c000e4758d8b0a9ef07f0abef" +
            "07f0c0ef06f0cbef06f01200848c000e426e000e416ef7ec0ff03b6e3b50036e046a8dbebeef07f0c2ef07f03b6a3b2a" +
            "c3ef07f03b6a3b50016e026ab0c03ff0b1c040f0000e3f16f00e401603500410d8b4d5ef07f0d7ef07f0d8ef07f04084" +
            "01500210d8b4dfef07f0e1ef07f0e2ef07f04086ff0e3b6e030e3c6ed0c03df0d1c03ef03b503d163c503e163d503f12" +
            "3e5040123fc0b0f040c0b1f060b0fbef07f0fdef07f077ef08f051ef08f0040e48ec10f00001e94bea2b77ef08f060b0" +
            "0cef08f00eef08f077ef08f00cec10f035c0b4f036c0b5f00001e94bea2b000e426e010e416e77ef08f0170e48ec10f0" +
            "02ef08f060b027ef08f029ef08f077ef08f00cec10f035c0b6f036c0b7f014ef08f0080e48ec10f002ef08f060b03bef" +
            "08f03def08f077ef08f00cec10f035c0b8f036c0b9f0000e0001ea6f020ee96f17ef08f0000e0001ea6f020ee96f77ef" +
            "08f0e9c03bf0eac03cf03c50000ad8b45cef08f04aef08f03b50020ad8b4ffef07f0010ad8b407ef08f0070ad8b41def" +
            "08f0010ad8b422ef08f0030ad8b431ef08f0010ad8b436ef08f04aef08f041504210d8b47eef08f080ef08f081ef08f0" +
            "849c41c039f042c03af01200000e3f6e000e3e6e000e416e000e406e000e436e000e426e000e3d6e000e3c6e00016251" +
            "376e63513e6e37c03ff06451376e6551406e37c041f028506019d8a4aaef08f0acef08f0eeef08f0000e436e000e426e" +
            "e1ef08f0d8904234d96e4334da6e680ed926000eda22df50376ed8904234d96e4334da6e670ed926000eda22df50386e" +
            "37503c6e38c03df042503e24376e43503f20386ed89037363836ac0e3724d96e000e3820da6e3cc0deff3dc0ddff424a" +
            "432a4050425c41504358d8a0eaef08f0ecef08f0b2ef08f058ef09f040503e24376e41503f20386e385009e10d0e375c" +
            "d8a0fdef08f0ffef08f006ef09f0100e356e030eceec0ef058ef09f0100e876f52c086f0000eab6f040eaa6f62c088f0" +
            "63c089f064c08af065c08bf0000e436e000e426e4bef09f0d8904234d96e4334da6e680ed926000eda22df50376ed890" +
            "4234d96e4334da6e670ed926000eda22df50386e37503c6e38c03df042503e24376e43503f20386ed89037363836ac0e" +
            "3724d96e000e3820da6e3cc0deff3dc0ddff424a432a4050425c41504358d8a054ef09f056ef09f01cef09f05dec0ff0" +
            "1200000e406e000e3f6e05500610d8b464ef09f066ef09f0feef09f024ec0df0feef09f0000e406e010e3f6e074a082a" +
            "085009e1020e075cd8a079ef09f07bef09f014ef0af0000e086e000e076e8a888a8a8a8ccf90ce68380ecd6e0e01ce91" +
            "cf80000e406e000e3f6e838c0001d24bd32b14ef0af00e01cea199ef09f09bef09f014ef0af0cf90ce68380ecd6ece91" +
            "cf80074a082a085009e1060e075cd8a0acef09f0aeef09f0c0ef09f0085007e1070e075cd8b0b7ef09f0b9ef09f0c0ef" +
            "09f0000e066e010e056e848cd9ef09f005040610d8a4c7ef09f0c9ef09f0d9ef09f0085009e1070e075cd8a0d2ef09f0" +
            "d4ef09f0d9ef09f0000e066e000e056e849c085009e1080e075cd8a0e2ef09f0e4ef09f014ef0af0000e086e000e076e" +
            "8a988a9a8a9c839c000e406e010e3f6e000e0001d36f000ed26f14ef0af0000e0001d36f000ed26f14ef0af0d2c03df0" +
            "d3c03ef03e50000ad8b409ef0af0f7ef09f03d50000ad8b46aef09f0010ad8b493ef09f0f7ef09f03fc03bf040c03cf0" +
            "1200b8ec06f04d6e030e4d18d8a423ef0af025ef0af062ef0af000c160f053685468530e356e000e366e0001605197ec" +
            "0df001c161f0530e356e000e366e0001615197ec0df0000e0001856f000e846f020e4e6e55ef0af0000e4e24d96eda6a" +
            "010eda22620e8425e16e000e8521e26edfcfe7ff844b852b4e2a29504e5cd8a05cef0af05eef0af044ef0af0020e266e" +
            "000e296e85ec0ff0020e2618d8a46bef0af06def0af0cbef0af00001855109e1020e845dd8a077ef0af079ef0af0cbef" +
            "0af0020e845f000e855b000e4e6e8fef0af0530e356e000e366e620e4e24d96eda6a000eda22df5097ec0df04e2a4e50" +
            "486e496a00018451485c85514958d8a09cef0af09eef0af081ef0af0630e8425d96e000e8521da6edf50486e48504a6e" +
            "496a620e8425d96e000e8521da6edf504b6e4c6a49504b264a504c2253504b1807e154504c18d8a4c0ef0af0c2ef0af0" +
            "c8ef0af00b4a0c2a010e256ec9ef0af000f0000e266e1200000e3c6e000e3b6e000e3e6e000e3d6e00016251376e6351" +
            "3b6e37c03cf06451376e65513d6e37c03ef028506019d8a4e8ef0af0eaef0af0ecef0af04cef0bf03d503b24376e3e50" +
            "3c20386e385009e10d0e375cd8a0fbef0af0fdef0af004ef0bf0030e356e020eceec0ef04cef0bf0030e876f52c086f0" +
            "000eab6f010eaa6f000e886f000e406e000e3f6e3fef0bf03f503b24376e40503c20386ed89037363836ac0e3724d96e" +
            "000e3820da6edecf39f0ddcf3af0880eaa25d96e000eab21da6e3a50df6e890eaa25d96e000eab21da6e39c0dfff020e" +
            "aa27000eab23aa05886f3f4a402a3d503f5c3e504058d8a048ef0bf04aef0bf014ef0bf05dec0ff0120055685668000e" +
            "106e000e0f6e220e0f24d96e010e1020da6e86c0dfff0f4a102a550e356e000e366e0001865197ec0df0220e0f24d96e" +
            "010e1020da6e87c0dfff0f4a102a550e356e000e366e0001875197ec0df0000e126e000e116e9cef0bf0880e1124d96e" +
            "000e1220da6e220e0f24e16e010e1020e26edfcfe7ff0f4a102a550e356e000e366e880e1124d96e000e1220da6edf50" +
            "97ec0df0114a122a0001aa51115cab511258d8a0a6ef0bf0a8ef0bf07def0bf0220e0f24d96e010e1020da6e55c0dfff" +
            "0f4a102a220e0f24d96e010e1020da6e5650df6e0f4a102ab6ec0ff0000e276e1200000e3c6e000e3b6e000e3a6e000e" +
            "396e00016251376e63513b6e37c03cf06451376e6551396e37c03af0060e876f52c086f0000eab6f040eaa6f28506019" +
            "d8a4e5ef0bf0e7ef0bf0f6ef0bf0d8903b34d96e3c34da6eac0ed926000eda2239c0deff3ac0ddff30ef0cf03c5009e1" +
            "0c0e3b5cd8a0ffef0bf001ef0cf008ef0cf0060e356e030eceec0ef02eef0cf0d8903b34d96e3c34da6eac0ed926000e" +
            "da2239c0deff3ac0ddff000e3d6e620e3d24d96eda6a000eda22880e3d24e16ee26a000ee222dfcfe7ff3d2a030e3d64" +
            "2cef0cf02eef0cf017ef0cf05dec0ff012002750d8b437ef0cf039ef0cf042ef0cf04dec0bf0000e0001e66f020ee56f" +
            "47ef0cf0000e0001e66f010ee56f19ec0af042ec10f00009d8b451ef0cf053ef0cf097ef0cf0525000016019d8b45bef" +
            "0cf05def0cf066ef0cf028506019d8a464ef0cf066ef0cf097ef0cf052506019d8a46def0cf06fef0cf086ef0cf0ba4b" +
            "bb2b86ef0cf0ccec0af095ef0cf0c1ec0bf095ef0cf086ec08f095ef0cf061c035f0010eceec0ef095ef0cf06151030a" +
            "d8b473ef0cf0050ad8b477ef0cf0160ad8b47bef0cf07fef0cf00b4a0c2ae5c04ff0e6c050f012002f6e9da6a2ef0cf0" +
            "a4ef0cf0d2ef0cf02e50d8b4aaef0cf0acef0cf0e1ef0cf052502f18d8b4b3ef0cf0b5ef0cf0beef0cf028502f18d8a4" +
            "bcef0cf0beef0cf0e1ef0cf09d96010e246e000e296e000e2924d96eda6a010eda222fc0dfff292a000e146e000e136e" +
            "e1ef0cf0220e2960d8ef0cf0daef0cf0dcef0cf0c3ef0cf0000e296e000e246e9d861200acc03df0adc03ef03eaeebef" +
            "0cf0edef0cf0f5ef0cf0858a8486000e3a6e010e396e23ef0df0859a8496ff0e3d143f6e030e3e14406e3f504010d8a4" +
            "04ef0df006ef0df00aef0df0000e406e010e3f6e3f500001cc1909e14050cd19d8b415ef0df017ef0df0efef0cf03fc0" +
            "35f040c036f003ec0ff03fc0ccf040c0cdf0efef0cf0120060b029ef0df02bef0df061ef0df04bef0df0020e48ec10f0" +
            "0001ce4bcf2b61ef0df060b03aef0df03cef0df061ef0df00cec10f0ff0e35140001d06f030e3614d16f000ecf6f000e" +
            "ce6f61ef0df0cec039f0cfc03af03a50000ad8b456ef0df061ef0df03950000ad8b42def0df0010ad8b435ef0df061ef" +
            "0df01200d80ef66e200ef76e000ef86e0900f5cfe9f00900f5cfeaf0c20ef66e200ef76e000ef86e00ee51f010ee06f0" +
            "0900f5cfeeffe550e150fae101ee00f0440eee6ae806fde100ee60f07b0eee6ae806fde100ee01f02d0eee6ae806fde1" +
            "58905892000ef86e000189ef04f0396e35c0d9ff36c0daffdecf37f0ddcf38f038c037f0386a3750776e35c0d9ff36c0" +
            "daffdf50766e39c074ff7c8c7cbab3ef0df0b5ef0df0aeef0df0765077cf37f0386a37c038f0376a3712000e381235c0" +
            "d9ff36c0daff37c0deff38c0ddff1200000e836e000e846e000e856e9f0e886e010e896ef90e8a6e800e0f01216f010e" +
            "196f1e0e116f000e2d6f000e186f000e106f000e206f000e0f6f000e176f000e1f6f0e6916691e69130e0e01b56f090e" +
            "f86f100eab6f070efd6f12000d4a0e2a85840a0ee82efed700d0000e3d6e000e3c6e0eef0ef03c503824d96e3d503920" +
            "da6edf5026ec10f03c4a3d2a3a503c5c3b503d58d8a017ef0ef019ef0ef003ef0ef09ea21eef0ef020ef0ef019ef0ef0" +
            "85940d4a0e2a1200920e7c6e7f0e7d6e000e766e000e776e000e0f014f6f000e4b6f000e4a6f000e496f3f0e4e6f4d69" +
            "4c69000e506f800e7b6e050e7a6e776876687d380f0b356e35280001d86f7d500f0b356e3528d96f1200000e6a6e000e" +
            "6b6e000e6c6e000e6d6e000e686e000e696e000e666e000e0f015f6f000e5d6f000e596f020e5a6f000e5b6f000e656e" +
            "000e586f000e566f3f0e576f840e606e5c691200000eca6e000ecb6e010ecc6efe0ec86e700ec76ec750c8cf37f0386a" +
            "37c038f0376a37100001e16f3850e26f0e01ce95c685000e356e000e366e58ec10f0010ec96e12006aec10f0c5ec0ff0" +
            "c8ec0df0e0ec0ff05dec10f024ec0ef04dec0ef01dec0ff0a7ec0ff098ec0ff002ec10f072ec0ef0ecec0ff0e9ec0ef0" +
            "b3ec0ef012000e01c59b000e356e000e366e53ec10f0080e9f6ed80e9d6e240e9e6e070e9b6e000e9c6e000e2d6e000e" +
            "2c6e000e2b6e0e01c58b1200366e285000016019d8a4d7ef0ef0d9ef0ef0dbef0ef0e8ef0ef035508009876f36c088f0" +
            "52c086f0000eab6f010eaa6f5dec0ff01200000ed06e000ed16e010ed26ece68380ecd6ecd50cecf35f0366a35c036f0" +
            "356a35100001dd6f3650de6f0e01ce91310ecf6e120035c037f036c038f0d89038323732d890383237323750a46e35c0" +
            "37f0030e3716060e386ed8903736382e15ef0ff03750a36e1200010eb96e000eb86e000eba6ed40eb66e000eb56e0e01" +
            "ce97c687000e356e000e366e4eec10f0f00eb76e1200010e0001cd6f8f0ecc6f010e366e8f0e356e03ec0ff08a988a9a" +
            "8a9ca58e858800018f0eac13010ead131200fe0ef66e7f0ef76e000ef86e0900f5cfd4f00001d56b0900f5500001d527" +
            "d4c035f0d5c036f012002750d8b463ef0ff065ef0ff067ef0ff070ef0ff00001bc4bbd2b000e126e000e116e010e276e" +
            "1200c9a476ef0ff078ef0ff080ef0ff0c9902f50c86e2ec0c7ffc98084ef0ff02f50c86e2ec0c7ff1200145009e1020e" +
            "135cd8a08eef0ff090ef0ff097ef0ff09d86000e146e000e136e000e296e1200040eb36e000eb26e000eb46e170eb06e" +
            "000eaf6e0e01ce9bf00eb16e1200010ebf6e040ebe6e000ec06ec70ebc6e000ebb6e0e01ce93a00ebd6e1200220e386e" +
            "010e396e0fc03af010c03bf0f6ec0df0000e106e000e0f6e1200000e0e01e16f000ee26f000ee36f000ee46f000ee56f" +
            "000ee66f1200366e9ea2d9ef0ff0dbef0ff0d4ef0ff09e909e9c36c09aff1200600e0e01d86f000eda6f000edc6f080e" +
            "de6f000edd6f1200000ea56e630ea46ec00ea36eae50cf0b1009ae6e12000f013db1fdef0ff000ef10f0010e01ef10f0" +
            "000e1200920e0f01396f000e3a6f010e3b6f060e3c6f12006450386e376a63503724356e000e3820366e1200396e3950" +
            "376e3550356e37c036f01200376e37c052f064ec10f067ec10f01200376e000e356e3750d3ec0ff01200000e356e000e" +
            "366e53ec10f01200000e356e000e366e58ec10f01200e1c02ef0e2c02ff071ec0ff0120025c035f0000e256e35501200" +
            "356e35c05fff608e6080120035c0dbf036c0dcf0120035c0e3f036c0e4f0120035c0dff036c0e0f01200b80e0f01316f" +
            "120001ffffffffff2dec10f0120034ec10f01200f29a12000200ffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" +
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff1f25");

    [Fact]
    public void LegacyImage_Pic32WordSum_Equals251F()
    {
        Assert.Equal(30720, LegacyImage.Length);

        // Replicates TrackController5/firmware/src/slavefwhandler.c FwFileDownload case 2:
        //   limit = sizeof(FwFile) - 2 (30718); sum little-endian words over [0..30717],
        //   compare data1/data2 against FwFile[30718]/FwFile[30719].
        ushort sum = 0;
        for (int i = 0; i < 30718; i += 2)
        {
            sum = (ushort)(sum + LegacyImage[i] + (LegacyImage[i + 1] << 8));
        }

        ushort embedded = (ushort)(LegacyImage[30718] | (LegacyImage[30719] << 8));
        Assert.Equal((ushort)0x251F, sum);
        Assert.Equal((ushort)0x251F, embedded);
    }

    [Fact]
    public async Task Command_BootloaderOnly_DownloadsLegacyImage_As480Packets_ByteExact()
    {
        var hexPath = WriteHexFile(LegacyImage);
        try
        {
            var validation = FirmwareArtifactVerifier.Validate(hexPath, expectedSha256Hex: null);
            Assert.True(validation.Success, validation.Error);
            Assert.Equal((ushort)0x251F, validation.Artifact!.EmbeddedChecksum);
            Assert.Equal((ushort)0x251F, validation.Artifact.RecomputedChecksum);

            var transport = new CapturingTransport { Responder = BuildBootloaderOnlyResponder() };
            var variables = new TrackApplicationVariables();
            await using var comm = new TrackCommClientAsync(transport, variables);
            await comm.StartAsync(realHardwareMode: false);

            var sendNext = new SendNextFwDataPacket(comm, validation.Artifact.Helpers);
            var command = new FlashOneAmplifierCommand(
                comm, variables, validation.Artifact.Helpers, sendNext,
                messageTimeout: TimeSpan.FromSeconds(5), flashTimeout: TimeSpan.FromSeconds(5));

            var result = await command.ExecuteBootloaderOnlyAsync(1);

            Assert.True(result.Success, result.Error);

            // Every FW-data packet (command 141) must be a 66-byte frame (HEADER + command + 64 data).
            var dataFrames = transport.SentFrames
                .Where(f => f.Length > 1 && f[1] == TrackCommand.FILEDOWNLOAD_STATE_FW_DATA_RECEIVE)
                .ToList();

            Assert.Equal(480, dataFrames.Count);
            Assert.All(dataFrames, f => Assert.Equal(66, f.Length));

            var reassembled = dataFrames.SelectMany(f => f.Skip(2)).ToArray();
            Assert.Equal(30720, reassembled.Length);
            Assert.Equal(LegacyImage, reassembled);
        }
        finally
        {
            TryDelete(hexPath);
        }
    }

    // ------------------------------------------------------------------ helpers

    private static byte[] HexToBytes(string hex)
    {
        var bytes = new byte[hex.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        return bytes;
    }

    /// <summary>Writes a valid Intel HEX for <paramref name="image"/> (1920 16-byte rows
    /// covering 0x800..0x7FFF, a 0x0C config-word record and EOF).</summary>
    private static string WriteHexFile(byte[] image)
    {
        const int bootOffset = 0x800;
        var sb = new StringBuilder();
        for (int addr = 0; addr < image.Length; addr += 16)
        {
            sb.Append(":10").Append((bootOffset + addr).ToString("X4")).Append("00");
            int recordSum = 0x10 + (((bootOffset + addr) >> 8) & 0xFF) + ((bootOffset + addr) & 0xFF);
            for (int i = 0; i < 16; i++)
            {
                byte b = image[addr + i];
                sb.Append(b.ToString("X2"));
                recordSum += b;
            }
            sb.Append(((byte)(-recordSum & 0xFF)).ToString("X2")).AppendLine();
        }
        // Dummy 12-byte config word (file-download path does not depend on it).
        sb.Append(":0C00000000");
        int configSum = 0x0C;
        var config = new byte[12];
        for (int i = 0; i < 12; i++) { config[i] = (byte)(0x80 + i); configSum += config[i]; sb.Append(config[i].ToString("X2")); }
        sb.Append(((byte)(-configSum & 0xFF)).ToString("X2")).AppendLine();
        sb.Append(":00000001FF").AppendLine();

        var path = Path.Combine(Path.GetTempPath(), $"legacy251F_{Guid.NewGuid():N}.hex");
        File.WriteAllText(path, sb.ToString());
        return path;
    }

    private static void TryDelete(string path)
    {
        try { File.Delete(path); } catch (IOException) { }
    }

    /// <summary>Captures every sent frame and replies from a scripted responder.</summary>
    private sealed class CapturingTransport : ITrackTransport
    {
        private readonly System.Threading.Channels.Channel<byte[]> _frames =
            System.Threading.Channels.Channel.CreateUnbounded<byte[]>();
        public List<byte[]> SentFrames { get; } = new();
        public Func<byte[], IEnumerable<byte[]>>? Responder { get; set; }

        public Task OpenAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task SendAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
        {
            var frame = buffer.AsSpan(offset, count).ToArray();
            SentFrames.Add(frame);
            if (Responder != null)
                foreach (var reply in Responder(frame))
                    _frames.Writer.TryWrite(reply);
            return Task.CompletedTask;
        }

        public async IAsyncEnumerable<byte[]> ReceiveAsync(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            while (await _frames.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
                while (_frames.Reader.TryRead(out var frame))
                    yield return frame;
        }

        public Task CloseAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    /// <summary>Scripted PIC32 replies for CONNECT -> version -> FW handler -> 480 data
    /// chunks -> checksum OK -> config word -> bootloader-only flash (132).</summary>
    private static Func<byte[], IEnumerable<byte[]>> BuildBootloaderOnlyResponder()
    {
        int chunkCount = 0;
        return frame =>
        {
            byte command = frame.Length > 1 ? frame[1] : (byte)0;
            switch (command)
            {
                case EnumClientCommands.CLIENT_CONNECTION_REQUEST: // 250
                    return new[] { Ctrl(TaskId.CONTROLLER, TaskStates.CONNECTED, TaskStates.DONE, TaskMessages.NONE) };
                case TrackCommand.GET_CONTROLLER_VERSION: // 113
                    return new[] { new byte[] { Enums.HEADER, TaskId.CONTROLLER, TrackCommand.GET_CONTROLLER_VERSION, 1, 0, 0, 0x01, 0 } };
                case TrackCommand.EXEC_MBUS_STATE_SLAVE_FW_FLASH: // 103
                    return new[] { Ctrl(TaskId.FWHANDLER, TrackCommand.FWHANDLERINIT, TaskStates.CONNECTED, TaskMessages.NONE) };
                case TrackCommand.EXEC_FW_STATE_RECEIVE_FW_FILE: // 120
                    return new[] { Ctrl(TrackCommand.FWFILEDOWNLOAD, TrackCommand.FILEDOWNLOAD_STATE_RECEIVE_FW_FILE_STANDBY, TaskStates.DONE, TaskMessages.NONE) };
                case TrackCommand.FILEDOWNLOAD_STATE_FW_DATA_RECEIVE: // 141
                    chunkCount++;
                    if (chunkCount >= 480)
                        return new[]
                        {
                            Ctrl(TrackCommand.FWFILEDOWNLOAD, TrackCommand.FILEDOWNLOAD_STATE_FW_DATA_DOWNLOAD_DONE, TaskStates.DONE, TaskMessages.NONE),
                            Ctrl(TrackCommand.FWFILEDOWNLOAD, TrackCommand.FILEDOWNLOAD_STATE_FW_CHECKSUM, TaskStates.DONE, TaskMessages.RECEIVED_CHECKSUM_OK),
                        };
                    return new[] { Ctrl(TrackCommand.FWFILEDOWNLOAD, TrackCommand.FILEDOWNLOAD_STATE_RECEIVE_FW_FILE_STANDBY, TaskStates.DONE, TaskMessages.NONE) };
                case TrackCommand.EXEC_FW_STATE_RECEIVE_CONFIG_WORD: // 121
                    return new[] { Ctrl(TrackCommand.FWCONFIGWORDDOWNLOAD, TrackCommand.CONFIGWORDDOWNLOAD_STATE_RECEIVE_CONFIG_WORD_STANDBY, TaskStates.DONE, TaskMessages.NONE) };
                case TrackCommand.CONFIGWORDDOWNLOAD_STATE_FW_CONFIG_WORD_RECEIVE: // 151
                    return new[] { Ctrl(TrackCommand.FWCONFIGWORDDOWNLOAD, TrackCommand.CONFIGWORDDOWNLOAD_STATE_FW_CONFIG_WORD_DOWNLOAD_DONE, TaskStates.DONE, TaskMessages.NONE) };
                case TrackCommand.EXEC_FW_STATE_FLASH_ONE_SLAVE_BOOTLOADER_ONLY: // 132
                    return new[]
                    {
                        Ctrl(TrackCommand.GET_BOOTLOADER_VERSION, TrackCommand.GET_BOOTLOADER_VERSION_OK, TaskStates.DONE, TaskMessages.NONE),
                        Ctrl(TrackCommand.ERASE_FLASH, TrackCommand.ERASE_FLASH_RETURNED_OK, TaskStates.DONE, TaskMessages.NONE),
                        Ctrl(TrackCommand.WRITE_FLASH, TrackCommand.WRITE_FLASH_RETURNED_OK, TaskStates.DONE, TaskMessages.NONE),
                        Ctrl(TrackCommand.WRITE_CONFIG, TrackCommand.WRITE_CONFIG_RETURNED_OK, TaskStates.DONE, TaskMessages.NONE),
                        Ctrl(TrackCommand.CHECK_CHECKSUM_CONFIG, TrackCommand.CHECK_CHECKSUM_CONFIG_RETURNED_OK, TaskStates.DONE, TaskMessages.NONE),
                        Ctrl(TrackCommand.RESET_SLAVE, TrackCommand.RESET_SLAVE_OK, TaskStates.DONE, TaskMessages.NONE),
                        Ctrl(TaskId.FWHANDLER, TrackCommand.EXEC_FW_STATE_FLASH_ONE_SLAVE_BOOTLOADER_ONLY, TaskStates.DONE, TaskMessages.NONE),
                    };
            }
            return Array.Empty<byte[]>();
        };
    }

    private static byte[] Ctrl(byte sender, byte command, byte state, byte message)
        => new byte[] { Enums.HEADER, sender, command, state, message };
}
