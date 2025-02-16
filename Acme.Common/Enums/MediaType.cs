using System.Runtime.Serialization;

namespace Acme.Common.Enums;

public enum MediaType
{
    [EnumMember(Value = "image/jpeg")]
    ImageJpeg,
    [EnumMember(Value = "image/png")]
    ImagePng,
    [EnumMember(Value = "image/gif")]
    ImageGif,
    [EnumMember(Value = "image/webp")]
    ImageWebp,
    [EnumMember(Value = "image/svg+xml")]
    ImageSvg,
    [EnumMember(Value = "image/tiff")]
    ImageTiff,
    [EnumMember(Value = "image/bmp")]
    ImageBmp,
    [EnumMember(Value = "text/plain")]
    TextPlain,
    [EnumMember(Value = "text/csv")]
    TextCsv,
    [EnumMember(Value = "audio/aac")]
    AudioAac,
    [EnumMember(Value = "audio/mpeg")]
    AudioMp3,
    [EnumMember(Value = "audio/ogg")]
    AudioOgg,
    [EnumMember(Value = "audio/wav")]
    AudioWav,
    [EnumMember(Value = "application/pdf")]
    ApplicationPdf,
    [EnumMember(Value = "application/json")]
    ApplicationJson,
    [EnumMember(Value = "application/xml")]
    ApplicationXml,
    [EnumMember(Value = "application/rtf")]
    ApplicationRtf,
    [EnumMember(Value = "application/zip")]
    ApplicationZip,
    [EnumMember(Value = "application/vnd.ms-excel")]
    ApplicationVndMsExcel,
    [EnumMember(Value = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    ApplicationVndOpenXmlFormatsOfficeDocumentSpreadsheetMlSheet,
    Unknown,
}
