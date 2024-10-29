using System.Text.Json.Serialization;
using GZCTF.Models.Internal;
using GZCTF.Models.Request.Account;
using GZCTF.Models.Request.Admin;
using GZCTF.Models.Request.Edit;
using GZCTF.Models.Request.Game;
using GZCTF.Models.Request.Info;
using GZCTF.Services.Container.Provider;

namespace GZCTF.Utils;

[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(HashSet<string>))]
[JsonSerializable(typeof(DockerRegistryOptions))]
[JsonSerializable(typeof(GameMetadata))]
[JsonSerializable(typeof(ExerciseMetadata))]
[JsonSerializable(typeof(RequestResponse))]
[JsonSerializable(typeof(RequestResponse<RegisterStatus>))]
[JsonSerializable(typeof(RequestResponse<bool>))]
[JsonSerializable(typeof(string))]
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
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(TaskStatus))]
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
[JsonSerializable(typeof(AnswerResult))]
[JsonSerializable(typeof(BasicWriteupInfoModel))]
[JsonSerializable(typeof(PostInfoModel[]))]
[JsonSerializable(typeof(ClientConfig))]
[JsonSerializable(typeof(ClientCaptchaInfoModel))]
[JsonSerializable(typeof(TeamInfoModel))]
[JsonSerializable(typeof(TeamInfoModel[]))]
internal sealed partial class AppJsonSerializerContext : JsonSerializerContext { }
