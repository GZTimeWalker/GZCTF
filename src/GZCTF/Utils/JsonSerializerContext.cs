using System.Text.Json;
using System.Text.Json.Serialization;
using GZCTF.Models.Internal;
using GZCTF.Models.Request.Account;
using GZCTF.Models.Request.Admin;
using GZCTF.Models.Request.Edit;
using GZCTF.Models.Request.Game;
using GZCTF.Models.Request.Info;
using GZCTF.Services.Container.Provider;
using Namotion.Reflection;
using NJsonSchema;
using NJsonSchema.Generation;
using NJsonSchema.Generation.TypeMappers;

namespace GZCTF.Utils;

[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(TaskStatus))]
[JsonSerializable(typeof(AnswerResult))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(HashSet<string>))]
[JsonSerializable(typeof(DockerRegistryOptions))]
[JsonSerializable(typeof(GameMetadata))]
[JsonSerializable(typeof(ExerciseMetadata))]
[JsonSerializable(typeof(RequestResponse))]
[JsonSerializable(typeof(RequestResponse<RegisterStatus>))]
[JsonSerializable(typeof(RequestResponse<bool>))]
[JsonSerializable(typeof(ProfileUserInfoModel))]
[JsonSerializable(typeof(ConfigEditModel))]
[JsonSerializable(typeof(ArrayResponse<UserInfoModel>))]
[JsonSerializable(typeof(ArrayResponse<TeamInfoModel>))]
[JsonSerializable(typeof(LogMessageModel[]))]
[JsonSerializable(typeof(WriteupInfoModel[]))]
[JsonSerializable(typeof(ArrayResponse<ContainerInstanceModel>))]
[JsonSerializable(typeof(ArrayResponse<LocalFile>))]
[JsonSerializable(typeof(List<LocalFile>))]
[JsonSerializable(typeof(PostDetailModel))]
[JsonSerializable(typeof(GameInfoModel))]
[JsonSerializable(typeof(ArrayResponse<GameInfoModel>))]
[JsonSerializable(typeof(GameNotice))]
[JsonSerializable(typeof(GameNotice[]))]
[JsonSerializable(typeof(ChallengeEditDetailModel))]
[JsonSerializable(typeof(ChallengeInfoModel[]))]
[JsonSerializable(typeof(ContainerInfoModel))]
[JsonSerializable(typeof(BasicGameInfoModel[]))]
[JsonSerializable(typeof(DetailedGameInfoModel))]
[JsonSerializable(typeof(ScoreboardModel))]
[JsonSerializable(typeof(GameEvent[]))]
[JsonSerializable(typeof(Submission[]))]
[JsonSerializable(typeof(CheatInfoModel[]))]
[JsonSerializable(typeof(ChallengeTrafficModel[]))]
[JsonSerializable(typeof(TeamTrafficModel[]))]
[JsonSerializable(typeof(FileRecord[]))]
[JsonSerializable(typeof(GameDetailModel))]
[JsonSerializable(typeof(ParticipationInfoModel[]))]
[JsonSerializable(typeof(ChallengeDetailModel))]
[JsonSerializable(typeof(BasicWriteupInfoModel))]
[JsonSerializable(typeof(PostInfoModel[]))]
[JsonSerializable(typeof(ClientConfig))]
[JsonSerializable(typeof(ClientCaptchaInfoModel))]
[JsonSerializable(typeof(TeamInfoModel))]
[JsonSerializable(typeof(TeamInfoModel[]))]
internal sealed partial class AppJsonSerializerContext : JsonSerializerContext;

public class DateTimeOffsetJsonConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.Number
            ? DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64())
            : reader.GetDateTimeOffset();

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options) =>
        writer.WriteNumberValue(value.ToUnixTimeMilliseconds());
}

public class OpenApiDateTimeOffsetToUIntMapper : ITypeMapper
{
    public void GenerateSchema(JsonSchema schema, TypeMapperContext context)
    {
        schema.Type = JsonObjectType.Integer;
        schema.Format = JsonFormatStrings.ULong;
    }

    public Type MappedType => typeof(DateTimeOffset);

    public bool UseReference => false;
}

// wait for https://github.com/RicoSuter/NJsonSchema/issues/1741
internal class GenericsSystemTextJsonReflectionService : SystemTextJsonReflectionService
{
    private static bool HasStringEnumConverter(ContextualType contextualType)
    {
        dynamic? jsonConverterAttribute = contextualType
            .GetContextOrTypeAttributes(true)?
            .FirstOrDefault(a => a.GetType().Name == "JsonConverterAttribute");

        if (jsonConverterAttribute == null ||
            !ObjectExtensions.HasProperty(jsonConverterAttribute, "ConverterType"))
            return false;

        if (jsonConverterAttribute?.ConverterType is Type converterType)
            return converterType.IsAssignableToTypeName("StringEnumConverter", TypeNameStyle.Name) ||
                   converterType.IsAssignableToTypeName("JsonStringEnumConverter`1", TypeNameStyle.Name) ||
                   converterType.IsAssignableToTypeName("System.Text.Json.Serialization.JsonStringEnumConverter",
                       TypeNameStyle.FullName);

        return false;
    }

    public override bool IsStringEnum(ContextualType contextualType, SystemTextJsonSchemaGeneratorSettings settings)
        => contextualType.TypeInfo.IsEnum && HasStringEnumConverter(contextualType);
}
