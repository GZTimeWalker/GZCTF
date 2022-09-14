/* eslint-disable */
/* tslint:disable */
/*
 * ---------------------------------------------------------------
 * ## THIS FILE WAS GENERATED VIA SWAGGER-TYPESCRIPT-API        ##
 * ##                                                           ##
 * ## AUTHOR: acacode                                           ##
 * ## SOURCE: https://github.com/acacode/swagger-typescript-api ##
 * ---------------------------------------------------------------
 */

import axios, { AxiosInstance, AxiosRequestConfig, AxiosResponse, ResponseType } from 'axios'
import useSWR, { mutate, MutatorOptions, SWRConfiguration } from 'swr'

/**
 * 请求响应
 */
export interface RequestResponseOfRegisterStatus {
  /** 响应信息 */
  title?: string

  /** 数据 */
  data?: RegisterStatus

  /**
   * 状态码
   * @format int32
   */
  status?: number
}

/**
 * 登录响应状态
 */
export enum RegisterStatus {
  LoggedIn = 'LoggedIn',
  AdminConfirmationRequired = 'AdminConfirmationRequired',
  EmailConfirmationRequired = 'EmailConfirmationRequired',
}

/**
 * 请求响应
 */
export interface RequestResponse {
  /** 响应信息 */
  title?: string

  /**
   * 状态码
   * @format int32
   */
  status?: number
}

/**
 * 注册账号
 */
export interface RegisterModel {
  /** 用户名 */
  userName: string

  /** 密码 */
  password: string

  /**
   * 邮箱
   * @format email
   */
  email: string

  /** Google Recaptcha Token */
  gToken?: string | null
}

/**
 * 找回账号
 */
export interface RecoveryModel {
  /**
   * 用户邮箱
   * @format email
   */
  email: string

  /** Google Recaptcha Token */
  gToken?: string | null
}

/**
 * 账号密码重置
 */
export interface PasswordResetModel {
  /** 密码 */
  password: string

  /** 邮箱 */
  email: string

  /** 邮箱接收到的Base64格式Token */
  rToken: string
}

/**
 * 账号验证
 */
export interface AccountVerifyModel {
  /** 邮箱接收到的Base64格式Token */
  token: string

  /** 用户邮箱的Base64格式 */
  email: string
}

/**
 * 登录
 */
export interface LoginModel {
  /** 用户名或邮箱 */
  userName: string

  /** 密码 */
  password: string
}

/**
 * 基本账号信息更改
 */
export interface ProfileUpdateModel {
  /** 用户名 */
  userName?: string | null

  /** 描述 */
  bio?: string | null

  /**
   * 手机号
   * @format phone
   */
  phone?: string | null

  /** 真实姓名 */
  realName?: string | null

  /** 学工号 */
  stdNumber?: string | null
}

/**
 * 密码更改
 */
export interface PasswordChangeModel {
  /** 旧密码 */
  old: string

  /** 新密码 */
  new: string
}

/**
 * 请求响应
 */
export interface RequestResponseOfBoolean {
  /** 响应信息 */
  title?: string

  /** 数据 */
  data?: boolean

  /**
   * 状态码
   * @format int32
   */
  status?: number
}

/**
 * 邮箱更改
 */
export interface MailChangeModel {
  /**
   * 新邮箱
   * @format email
   */
  newMail: string
}

/**
 * 基本账号信息
 */
export interface ProfileUserInfoModel {
  /** 用户ID */
  userId?: string | null

  /** 用户名 */
  userName?: string | null

  /** 邮箱 */
  email?: string | null

  /** 签名 */
  bio?: string | null

  /** 手机号码 */
  phone?: string | null

  /** 真实姓名 */
  realName?: string | null

  /** 学工号 */
  stdNumber?: string | null

  /** 头像链接 */
  avatar?: string | null

  /** 用户角色 */
  role?: Role | null
}

/**
 * 用户权限枚举
 */
export enum Role {
  Banned = 'Banned',
  User = 'User',
  Monitor = 'Monitor',
  Admin = 'Admin',
}

/**
 * 全局配置更新对象
 */
export interface ConfigEditModel {
  /** 用户策略 */
  accountPolicy?: AccountPolicy | null

  /** 全局配置项 */
  globalConfig?: GlobalConfig | null
}

/**
 * 账户策略
 */
export interface AccountPolicy {
  /** 允许用户注册 */
  allowRegister?: boolean

  /** 注册时直接激活账户 */
  activeOnRegister?: boolean

  /** 使用谷歌验证码校验 */
  useGoogleRecaptcha?: boolean

  /** 注册、更换邮箱、找回密码需要邮件确认 */
  emailConfirmationRequired?: boolean

  /** 邮箱后缀域名，以逗号分割 */
  emailDomainList?: string
}

/**
 * 全局设置
 */
export interface GlobalConfig {
  /** 平台前缀名称 */
  title?: string
}

/**
 * 用户信息（Admin）
 */
export interface UserInfoModel {
  /** 用户ID */
  id?: string | null

  /** 用户名 */
  userName?: string | null

  /** 真实姓名 */
  realName?: string | null

  /** 学号 */
  stdNumber?: string | null

  /** 联系电话 */
  phone?: string | null

  /** 签名 */
  bio?: string | null

  /**
   * 注册时间
   * @format date-time
   */
  registerTimeUTC?: string

  /**
   * 用户最近访问时间
   * @format date-time
   */
  lastVisitedUTC?: string

  /** 用户最近访问IP */
  ip?: string

  /** 邮箱 */
  email?: string | null

  /** 头像链接 */
  avatar?: string | null

  /** 用户角色 */
  role?: Role | null

  /** 用户是否通过邮箱验证（可登录） */
  emailConfirmed?: boolean | null
}

/**
 * 队伍信息
 */
export interface TeamInfoModel {
  /**
   * 队伍 Id
   * @format int32
   */
  id?: number

  /** 队伍名称 */
  name?: string | null

  /** 队伍签名 */
  bio?: string | null

  /** 头像链接 */
  avatar?: string | null

  /** 是否锁定 */
  locked?: boolean

  /** 队伍成员 */
  members?: TeamUserInfoModel[] | null
}

/**
 * 队员信息
 */
export interface TeamUserInfoModel {
  /** 用户ID */
  id?: string | null

  /** 用户名 */
  userName?: string | null

  /** 签名 */
  bio?: string | null

  /** 头像链接 */
  avatar?: string | null

  /** 是否是队长 */
  captain?: boolean
}

/**
 * 用户信息更改（Admin）
 */
export interface UpdateUserInfoModel {
  /** 用户名 */
  userName?: string | null

  /**
   * 邮箱
   * @format email
   */
  email?: string | null

  /** 签名 */
  bio?: string | null

  /**
   * 手机号码
   * @format phone
   */
  phone?: string | null

  /** 真实姓名 */
  realName?: string | null

  /** 学工号 */
  stdNumber?: string | null

  /** 用户是否通过邮箱验证（可登录） */
  emailConfirmed?: boolean | null

  /** 用户角色 */
  role?: Role | null
}

/**
 * 日志信息（Admin）
 */
export interface LogMessageModel {
  /**
   * 日志时间
   * @format date-time
   */
  time?: string

  /** 用户名 */
  name?: string | null
  level?: string | null

  /** IP地址 */
  ip?: string | null

  /** 日志信息 */
  msg?: string | null

  /** 任务状态 */
  status?: string | null
}

export enum ParticipationStatus {
  Pending = 'Pending',
  Accepted = 'Accepted',
  Denied = 'Denied',
  Forfeited = 'Forfeited',
  Unsubmitted = 'Unsubmitted',
}

export interface LocalFile {
  /** 文件哈希 */
  hash?: string

  /** 文件名 */
  name: string
}

export interface ProblemDetails {
  type?: string | null
  title?: string | null

  /** @format int32 */
  status?: number | null
  detail?: string | null
  instance?: string | null
}

/**
 * 文章对象（Edit）
 */
export interface PostEditModel {
  /** 通知标题 */
  title: string

  /** 文章总结 */
  summary?: string

  /** 文章内容 */
  content?: string

  /** 文章标签 */
  tags?: string[]

  /** 是否置顶 */
  isPinned?: boolean
}

/**
 * 文章详细内容
 */
export interface PostDetailModel {
  /** 文章 Id */
  id: string

  /** 通知标题 */
  title: string

  /** 文章总结 */
  summary: string

  /** 文章内容 */
  content: string

  /** 是否置顶 */
  isPinned: boolean

  /** 文章标签 */
  tags?: string[] | null

  /** 作者头像 */
  autherAvatar?: string | null

  /** 作者名称 */
  autherName?: string | null

  /**
   * 发布时间
   * @format date-time
   */
  time: string
}

/**
 * 比赛信息（Edit）
 */
export interface GameInfoModel {
  /**
   * 比赛 Id
   * @format int32
   */
  id?: number

  /** 比赛标题 */
  title: string

  /** 是否隐藏 */
  hidden?: boolean

  /** 比赛描述 */
  summary?: string

  /** 比赛详细介绍 */
  content?: string

  /** 报名队伍免审核 */
  acceptWithoutReview?: boolean

  /** 比赛邀请码 */
  inviteCode?: string | null

  /** 参赛所属单位列表 */
  organizations?: string[] | null

  /**
   * 队员数量限制, 0 为无上限
   * @format int32
   */
  teamMemberCountLimit?: number

  /**
   * 队伍同时开启的容器数量限制
   * @format int32
   */
  containerCountLimit?: number

  /** 比赛头图 */
  poster?: string | null

  /** 比赛签名公钥 */
  publicKey?: string

  /** 比赛是否为练习模式（比赛结束够依然可以访问） */
  practiceMode?: boolean

  /**
   * 开始时间
   * @format date-time
   */
  start: string

  /**
   * 结束时间
   * @format date-time
   */
  end: string
}

/**
* 比赛通知，会发往客户端。
信息涵盖一二三血通知、提示发布通知、题目开启通知等
*/
export interface GameNotice {
  /** @format int32 */
  id: number

  /** 通知类型 */
  type: NoticeType

  /** 通知内容 */
  content: string

  /**
   * 发布时间
   * @format date-time
   */
  time: string
}

/**
 * 比赛公告类型
 */
export enum NoticeType {
  Normal = 'Normal',
  FirstBlood = 'FirstBlood',
  SecondBlood = 'SecondBlood',
  ThirdBlood = 'ThirdBlood',
  NewHint = 'NewHint',
  NewChallenge = 'NewChallenge',
}

/**
 * 比赛通知（Edit）
 */
export interface GameNoticeModel {
  /** 通知内容 */
  content: string
}

/**
 * 题目详细信息（Edit）
 */
export interface ChallengeEditDetailModel {
  /**
   * 题目Id
   * @format int32
   */
  id?: number

  /** 题目名称 */
  title: string

  /** 题目内容 */
  content?: string

  /** 题目标签 */
  tag: ChallengeTag

  /** 题目类型 */
  type: ChallengeType

  /** 题目提示 */
  hints?: string[]

  /** Flag 模版，用于根据 Token 和题目、比赛信息生成 Flag */
  flagTemplate?: string | null

  /** 是否启用题目 */
  isEnabled: boolean

  /** 镜像名称与标签 */
  containerImage: string

  /**
   * 运行内存限制 (MB)
   * @format int32
   */
  memoryLimit: number

  /**
   * CPU 运行数量限制
   * @format int32
   */
  cpuCount: number

  /**
   * 镜像暴露端口
   * @format int32
   */
  containerExposePort: number

  /**
   * 初始分数
   * @format int32
   */
  originalScore: number

  /**
   * 最低分数比例
   * @format double
   * @min 0
   * @max 1
   */
  minScoreRate: number

  /**
   * 难度系数
   * @format double
   */
  difficulty: number

  /**
   * 通过人数
   * @format int32
   */
  acceptedCount: number

  /** 统一文件名（仅用于动态附件） */
  fileName?: string | null

  /** 题目附件（动态附件存放于 FlagInfoModel） */
  attachment?: Attachment | null

  /** 测试容器 */
  testContainer?: ContainerInfoModel | null

  /** 题目 Flag 信息 */
  flags: FlagInfoModel[]
}

/**
 * 题目标签
 */
export enum ChallengeTag {
  Misc = 'Misc',
  Crypto = 'Crypto',
  Pwn = 'Pwn',
  Web = 'Web',
  Reverse = 'Reverse',
  Blockchain = 'Blockchain',
  Forensics = 'Forensics',
  Hardware = 'Hardware',
  Mobile = 'Mobile',
  PPC = 'PPC',
}

export enum ChallengeType {
  StaticAttachment = 'StaticAttachment',
  StaticContainer = 'StaticContainer',
  DynamicAttachment = 'DynamicAttachment',
  DynamicContainer = 'DynamicContainer',
}

export interface Attachment {
  /** @format int32 */
  id: number

  /** 附件类型 */
  type: FileType

  /** Flag 对应附件 (远程文件） */
  remoteUrl?: string | null

  /**
   * 本地文件 Id
   * @format int32
   */
  localFileId?: number | null

  /** Flag 对应文件（本地文件） */
  localFile?: LocalFile | null

  /** 文件默认 Url */
  url?: string | null
}

export enum FileType {
  None = 'None',
  Local = 'Local',
  Remote = 'Remote',
}

export interface ContainerInfoModel {
  /** 容器状态 */
  status?: ContainerStatus

  /**
   * 容器创建时间
   * @format date-time
   */
  startedAt?: string

  /**
   * 容器期望终止时间
   * @format date-time
   */
  expectStopAt?: string

  /** 题目入口 */
  entry?: string
}

/**
 * 容器状态
 */
export enum ContainerStatus {
  Pending = 'Pending',
  Running = 'Running',
  Destroyed = 'Destroyed',
}

/**
 * Flag 信息（Edit）
 */
export interface FlagInfoModel {
  /**
   * Flag Id
   * @format int32
   */
  id?: number

  /** Flag文本 */
  flag?: string

  /** Flag 对应附件 */
  attachment?: Attachment | null
}

/**
 * 基础题目信息（Edit）
 */
export interface ChallengeInfoModel {
  /**
   * 题目Id
   * @format int32
   */
  id?: number

  /** 题目名称 */
  title: string

  /** 题目标签 */
  tag?: ChallengeTag

  /** 题目类型 */
  type?: ChallengeType

  /** 是否启用题目 */
  isEnabled?: boolean

  /**
   * 题目分值
   * @format int32
   */
  score?: number

  /**
   * 最低分值
   * @format int32
   */
  minScore?: number

  /**
   * 最初分值
   * @format int32
   */
  originalScore?: number
}

/**
 * 题目更新信息（Edit）
 */
export interface ChallengeUpdateModel {
  /** 题目名称 */
  title?: string | null

  /** 题目内容 */
  content?: string | null

  /** Flag 模版，用于根据 Token 和题目、比赛信息生成 Flag */
  flagTemplate?: string | null

  /** 题目标签 */
  tag?: ChallengeTag | null

  /** 题目提示 */
  hints?: string[] | null

  /** 是否启用题目 */
  isEnabled?: boolean | null

  /** 镜像名称与标签 */
  containerImage?: string | null

  /**
   * 运行内存限制 (MB)
   * @format int32
   */
  memoryLimit?: number | null

  /**
   * CPU 运行数量限制
   * @format int32
   */
  cpuCount?: number | null

  /**
   * 镜像暴露端口
   * @format int32
   */
  containerExposePort?: number | null

  /**
   * 初始分数
   * @format int32
   */
  originalScore?: number | null

  /**
   * 最低分数比例
   * @format double
   * @min 0
   * @max 1
   */
  minScoreRate?: number | null

  /**
   * 难度系数
   * @format double
   */
  difficulty?: number | null

  /** 统一文件名 */
  fileName?: string | null
}

/**
 * 新建附件信息（Edit）
 */
export interface AttachmentCreateModel {
  /** 附件类型 */
  attachmentType?: FileType

  /** 文件哈希（本地文件） */
  fileHash?: string | null

  /** 文件 Url（远程文件） */
  remoteUrl?: string | null
}

/**
 * 新建 Flag 信息（Edit）
 */
export interface FlagCreateModel {
  /** Flag文本 */
  flag: string

  /** 附件类型 */
  attachmentType?: FileType

  /** 文件哈希（本地文件） */
  fileHash?: string | null

  /** 文件 Url（远程文件） */
  remoteUrl?: string | null
}

/**
 * 任务执行状态
 */
export enum TaskStatus {
  Success = 'Success',
  Fail = 'Fail',
  Duplicate = 'Duplicate',
  Denied = 'Denied',
  NotFound = 'NotFound',
  Exit = 'Exit',
  Pending = 'Pending',
}

/**
 * 比赛基本信息，不包含详细介绍与当前队伍报名状态
 */
export interface BasicGameInfoModel {
  /** @format int32 */
  id?: number

  /** 比赛标题 */
  title?: string

  /** 比赛描述 */
  summary?: string

  /** 头图 */
  poster?: string | null

  /**
   * 队员数量限制
   * @format int32
   */
  limit?: number

  /**
   * 开始时间
   * @format date-time
   */
  start?: string

  /**
   * 结束时间
   * @format date-time
   */
  end?: string
}

/**
 * 比赛详细信息，包含详细介绍与当前队伍报名状态
 */
export interface GameDetailModel {
  /** @format int32 */
  id?: number

  /** 比赛标题 */
  title?: string

  /** 比赛描述 */
  summary?: string

  /** 比赛详细介绍 */
  content?: string

  /** 是否为隐藏比赛 */
  hidden?: boolean

  /** 参赛所属单位列表 */
  organizations?: string[] | null

  /** 是否需要邀请码 */
  inviteCodeRequired?: boolean

  /** 比赛头图 */
  poster?: string | null

  /**
   * 队员数量限制
   * @format int32
   */
  limit?: number

  /**
   * 报名参赛队伍数量
   * @format int32
   */
  teamCount?: number

  /** 当前报名的组织 */
  organization?: string | null

  /** 参赛队伍名称 */
  teamName?: string | null

  /** 比赛是否为练习模式（比赛结束够依然可以访问） */
  practiceMode?: boolean

  /** 队伍参与状态 */
  status?: ParticipationStatus

  /**
   * 开始时间
   * @format date-time
   */
  start?: string

  /**
   * 结束时间
   * @format date-time
   */
  end?: string
}

export interface GameJoinModel {
  /**
   * 参赛队伍 Id
   * @format int32
   */
  teamId?: number

  /** 参赛单位 */
  organization?: string | null

  /** 参赛邀请码 */
  inviteCode?: string | null
}

/**
 * 排行榜
 */
export interface ScoreboardModel {
  /**
   * 更新时间
   * @format date-time
   */
  updateTimeUTC?: string

  /** 参赛组织 */
  organizations?: string[] | null

  /** 前十名的时间线 */
  timeLine?: TopTimeLine[]

  /** 队伍信息 */
  items?: ScoreboardItem[]

  /** 题目信息 */
  challenges?: Record<string, ChallengeInfo[]>
}

export interface TopTimeLine {
  /**
   * 队伍Id
   * @format int32
   */
  id?: number

  /** 队伍名称 */
  name?: string

  /** 时间线 */
  items?: TimeLine[]
}

export interface TimeLine {
  /**
   * 时间
   * @format date-time
   */
  time?: string

  /**
   * 得分
   * @format int32
   */
  score?: number
}

export interface ScoreboardItem {
  /**
   * 队伍Id
   * @format int32
   */
  id?: number

  /** 队伍名称 */
  name?: string

  /** 参赛所属组织 */
  organization?: string | null

  /** 队伍头像 */
  avatar?: string | null

  /**
   * 分数
   * @format int32
   */
  score?: number

  /**
   * 排名
   * @format int32
   */
  rank?: number

  /**
   * 已解出的题目数量
   * @format int32
   */
  solvedCount?: number

  /** 题目情况列表 */
  challenges?: ChallengeItem[]
}

export interface ChallengeItem {
  /**
   * 题目 Id
   * @format int32
   */
  id?: number

  /**
   * 题目分值
   * @format int32
   */
  score?: number

  /** 未解出、一血、二血、三血或者其他 */
  type?: SubmissionType

  /** 解题用户名 */
  userName?: string | null

  /**
   * 题目提交的时间，为了计算时间线
   * @format date-time
   */
  time?: string | null
}

/**
 * 提交类型
 */
export enum SubmissionType {
  Unaccepted = 'Unaccepted',
  FirstBlood = 'FirstBlood',
  SecondBlood = 'SecondBlood',
  ThirdBlood = 'ThirdBlood',
  Normal = 'Normal',
}

export interface ChallengeInfo {
  /**
   * 题目Id
   * @format int32
   */
  id?: number

  /** 题目名称 */
  title?: string

  /** 题目标签 */
  tag?: ChallengeTag

  /**
   * 题目分值
   * @format int32
   */
  score?: number

  /**
   * 解出队伍数量
   * @format int32
   */
  solved?: number

  /** 题目三血 */
  bloods?: (Blood | null)[]
}

export interface Blood {
  /**
   * 队伍Id
   * @format int32
   */
  id?: number

  /** 队伍名称 */
  name?: string

  /** 队伍头像 */
  avatar?: string | null

  /**
   * 获得此血的时间
   * @format date-time
   */
  submitTimeUTC?: string | null
}

/**
* 比赛事件，记录但不会发往客户端。
信息涵盖Flag提交信息、容器启动关闭信息、作弊信息、题目分数变更信息
*/
export interface GameEvent {
  /** 事件类型 */
  type: EventType

  /** 事件内容 */
  content: string

  /**
   * 发布时间
   * @format date-time
   */
  time: string

  /** 相关用户名 */
  user?: string

  /** 相关队伍名 */
  team?: string
}

/**
 * 比赛事件类型
 */
export enum EventType {
  Normal = 'Normal',
  ContainerStart = 'ContainerStart',
  ContainerDestroy = 'ContainerDestroy',
  FlagSubmit = 'FlagSubmit',
  CheatDetected = 'CheatDetected',
}

export interface Submission {
  /** 提交的答案字符串 */
  answer?: string

  /** 提交的答案状态 */
  status?: AnswerResult

  /**
   * 答案提交的时间
   * @format date-time
   */
  time?: string

  /** 提交用户 */
  user?: string

  /** 提交队伍 */
  team?: string
}

/**
 * 判定结果
 */
export enum AnswerResult {
  FlagSubmitted = 'FlagSubmitted',
  Accepted = 'Accepted',
  WrongAnswer = 'WrongAnswer',
  NotFound = 'NotFound',
  CheatDetected = 'CheatDetected',
}

export interface GameTeamDetailModel {
  /** 积分榜信息 */
  rank?: ScoreboardItem | null

  /** 队伍 Token */
  teamToken: string
}

/**
 * 比赛参与对象，用于审核查看（Admin）
 */
export interface ParticipationInfoModel {
  /**
   * 参与对象 Id
   * @format int32
   */
  id?: number

  /** 参与队伍 */
  team?: TeamWithDetailedUserInfo

  /** 注册的成员 */
  registeredMembers?: string[]

  /** 参赛所属组织 */
  organization?: string | null

  /** 参与状态 */
  status?: ParticipationStatus
}

/**
 * 比赛队伍详细信息，用于审核查看（Admin）
 */
export interface TeamWithDetailedUserInfo {
  /**
   * 队伍 Id
   * @format int32
   */
  id?: number

  /** 队伍名称 */
  name?: string | null

  /** 队伍签名 */
  bio?: string | null

  /** 头像链接 */
  avatar?: string | null

  /** 是否锁定 */
  locked?: boolean

  /** 队长 Id */
  captainId?: string

  /** 队伍成员 */
  members?: ProfileUserInfoModel[] | null
}

/**
 * 题目详细信息
 */
export interface ChallengeDetailModel {
  /**
   * 题目 Id
   * @format int32
   */
  id?: number

  /** 题目名称 */
  title?: string

  /** 题目内容 */
  content?: string

  /** 题目标签 */
  tag?: ChallengeTag

  /** 题目提示 */
  hints?: string[] | null

  /**
   * 题目当前分值
   * @format int32
   */
  score?: number

  /** 题目类型 */
  type?: ChallengeType

  /** Flag 上下文 */
  context?: ClientFlagContext
}

export interface ClientFlagContext {
  /**
   * 题目实例的关闭时间
   * @format date-time
   */
  closeTime?: string | null

  /** 题目实例的连接方式 */
  instanceEntry?: string | null

  /** 附件 Url */
  url?: string | null
}

/**
 * flag 提交
 */
export interface FlagSubmitModel {
  /**
   * flag 内容
   * fix: 防止前端的意外提交 (number/float/null) 可能被错误转换
   */
  flag: string
}

/**
 * 文章信息
 */
export interface PostInfoModel {
  /** 文章 Id */
  id: string

  /** 文章标题 */
  title: string

  /** 文章总结 */
  summary: string

  /** 是否置顶 */
  isPinned: boolean

  /** 文章标签 */
  tags?: string[] | null

  /** 作者头像 */
  autherAvatar?: string | null

  /** 作者名称 */
  autherName?: string | null

  /**
   * 更新时间
   * @format date-time
   */
  time: string
}

/**
 * 队伍信息更新
 */
export interface TeamUpdateModel {
  /** 队伍名称 */
  name?: string | null

  /** 队伍签名 */
  bio?: string | null
}

export interface TeamTransferModel {
  /** 新队长 Id */
  newCaptainId: string
}

export type QueryParamsType = Record<string | number, any>

export interface FullRequestParams
  extends Omit<AxiosRequestConfig, 'data' | 'params' | 'url' | 'responseType'> {
  /** set parameter to `true` for call `securityWorker` for this request */
  secure?: boolean
  /** request path */
  path: string
  /** content type of request body */
  type?: ContentType
  /** query params */
  query?: QueryParamsType
  /** format of response (i.e. response.json() -> format: "json") */
  format?: ResponseType
  /** request body */
  body?: unknown
}

export type RequestParams = Omit<FullRequestParams, 'body' | 'method' | 'query' | 'path'>

export interface ApiConfig<SecurityDataType = unknown>
  extends Omit<AxiosRequestConfig, 'data' | 'cancelToken'> {
  securityWorker?: (
    securityData: SecurityDataType | null
  ) => Promise<AxiosRequestConfig | void> | AxiosRequestConfig | void
  secure?: boolean
  format?: ResponseType
}

export enum ContentType {
  Json = 'application/json',
  FormData = 'multipart/form-data',
  UrlEncoded = 'application/x-www-form-urlencoded',
}

export class HttpClient<SecurityDataType = unknown> {
  public instance: AxiosInstance
  private securityData: SecurityDataType | null = null
  private securityWorker?: ApiConfig<SecurityDataType>['securityWorker']
  private secure?: boolean
  private format?: ResponseType

  constructor({
    securityWorker,
    secure,
    format,
    ...axiosConfig
  }: ApiConfig<SecurityDataType> = {}) {
    this.instance = axios.create({ ...axiosConfig, baseURL: axiosConfig.baseURL || '' })
    this.secure = secure
    this.format = format
    this.securityWorker = securityWorker
  }

  public setSecurityData = (data: SecurityDataType | null) => {
    this.securityData = data
  }

  private mergeRequestParams(
    params1: AxiosRequestConfig,
    params2?: AxiosRequestConfig
  ): AxiosRequestConfig {
    return {
      ...this.instance.defaults,
      ...params1,
      ...(params2 || {}),
      headers: Object.assign(
        {},
        this.instance.defaults.headers,
        (params1 || {}).headers,
        (params2 || {}).headers
      ),
    }
  }

  private createFormData(input: Record<string, unknown>): FormData {
    return Object.keys(input || {}).reduce((formData, key) => {
      const property = input[key]
      if (Array.isArray(property)) {
        property.forEach((blob) => {
          formData.append(
            key,
            blob instanceof Blob
              ? blob
              : typeof blob === 'object' && blob !== null
              ? JSON.stringify(blob)
              : `${blob}`
          )
        })
      } else {
        formData.append(
          key,
          property instanceof Blob
            ? property
            : typeof property === 'object' && property !== null
            ? JSON.stringify(property)
            : `${property}`
        )
      }
      return formData
    }, new FormData())
  }

  public request = async <T = any, _E = any>({
    secure,
    path,
    type,
    query,
    format,
    body,
    ...params
  }: FullRequestParams): Promise<AxiosResponse<T>> => {
    const secureParams =
      ((typeof secure === 'boolean' ? secure : this.secure) &&
        this.securityWorker &&
        (await this.securityWorker(this.securityData))) ||
      {}
    const requestParams = this.mergeRequestParams(params, secureParams)
    const responseFormat = (format && this.format) || void 0

    if (type === ContentType.FormData && body && body !== null && typeof body === 'object') {
      if (!requestParams.headers) requestParams.headers = { Accept: '*/*' }

      body = this.createFormData(body as Record<string, unknown>)
    }

    return this.instance.request({
      ...requestParams,
      headers: {
        ...(type && type !== ContentType.FormData ? { 'Content-Type': type } : {}),
        ...(requestParams.headers || {}),
      },
      params: query,
      responseType: responseFormat,
      data: body,
      url: path,
    })
  }
}

/**
 * @title GZCTF Server API
 * @version v1
 *
 * GZCTF Server 接口文档
 */
export class Api<SecurityDataType extends unknown> extends HttpClient<SecurityDataType> {
  account = {
    /**
     * @description 使用此接口注册新用户，Dev环境下不校验 GToken，邮件URL：/verify
     *
     * @tags Account
     * @name AccountRegister
     * @summary 用户注册接口
     * @request POST:/api/account/register
     */
    accountRegister: (data: RegisterModel, params: RequestParams = {}) =>
      this.request<RequestResponseOfRegisterStatus, RequestResponse>({
        path: `/api/account/register`,
        method: 'POST',
        body: data,
        type: ContentType.Json,
        format: 'json',
        ...params,
      }),

    /**
     * @description 使用此接口请求找回密码，向用户邮箱发送邮件，邮件URL：/reset
     *
     * @tags Account
     * @name AccountRecovery
     * @summary 用户找回密码请求接口
     * @request POST:/api/account/recovery
     */
    accountRecovery: (data: RecoveryModel, params: RequestParams = {}) =>
      this.request<RequestResponse, RequestResponse>({
        path: `/api/account/recovery`,
        method: 'POST',
        body: data,
        type: ContentType.Json,
        format: 'json',
        ...params,
      }),

    /**
     * @description 使用此接口重置密码，需要邮箱验证码
     *
     * @tags Account
     * @name AccountPasswordReset
     * @summary 用户重置密码接口
     * @request POST:/api/account/passwordreset
     */
    accountPasswordReset: (data: PasswordResetModel, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/account/passwordreset`,
        method: 'POST',
        body: data,
        type: ContentType.Json,
        ...params,
      }),

    /**
     * @description 使用此接口通过邮箱验证码确认邮箱
     *
     * @tags Account
     * @name AccountVerify
     * @summary 用户邮箱确认接口
     * @request POST:/api/account/verify
     */
    accountVerify: (data: AccountVerifyModel, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/account/verify`,
        method: 'POST',
        body: data,
        type: ContentType.Json,
        ...params,
      }),

    /**
     * @description 使用此接口登录账户
     *
     * @tags Account
     * @name AccountLogIn
     * @summary 用户登录接口
     * @request POST:/api/account/login
     */
    accountLogIn: (data: LoginModel, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/account/login`,
        method: 'POST',
        body: data,
        type: ContentType.Json,
        ...params,
      }),

    /**
     * @description 使用此接口登出账户，需要User权限
     *
     * @tags Account
     * @name AccountLogOut
     * @summary 用户登出接口
     * @request POST:/api/account/logout
     */
    accountLogOut: (params: RequestParams = {}) =>
      this.request<void, any>({
        path: `/api/account/logout`,
        method: 'POST',
        ...params,
      }),

    /**
     * @description 使用此接口更新用户用户名和描述，需要User权限
     *
     * @tags Account
     * @name AccountUpdate
     * @summary 用户数据更新接口
     * @request PUT:/api/account/update
     */
    accountUpdate: (data: ProfileUpdateModel, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/account/update`,
        method: 'PUT',
        body: data,
        type: ContentType.Json,
        ...params,
      }),

    /**
     * @description 使用此接口更新用户密码，需要User权限
     *
     * @tags Account
     * @name AccountChangePassword
     * @summary 用户密码更改接口
     * @request PUT:/api/account/changepassword
     */
    accountChangePassword: (data: PasswordChangeModel, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/account/changepassword`,
        method: 'PUT',
        body: data,
        type: ContentType.Json,
        ...params,
      }),

    /**
     * @description 使用此接口更改用户邮箱，需要User权限，邮件URL：/confirm
     *
     * @tags Account
     * @name AccountChangeEmail
     * @summary 用户邮箱更改接口
     * @request PUT:/api/account/changeemail
     */
    accountChangeEmail: (data: MailChangeModel, params: RequestParams = {}) =>
      this.request<RequestResponseOfBoolean, RequestResponse>({
        path: `/api/account/changeemail`,
        method: 'PUT',
        body: data,
        type: ContentType.Json,
        format: 'json',
        ...params,
      }),

    /**
     * @description 使用此接口确认更改用户邮箱，需要邮箱验证码，需要User权限
     *
     * @tags Account
     * @name AccountMailChangeConfirm
     * @summary 用户邮箱更改确认接口
     * @request POST:/api/account/mailchangeconfirm
     */
    accountMailChangeConfirm: (data: AccountVerifyModel, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/account/mailchangeconfirm`,
        method: 'POST',
        body: data,
        type: ContentType.Json,
        ...params,
      }),

    /**
     * @description 使用此接口获取用户信息，需要User权限
     *
     * @tags Account
     * @name AccountProfile
     * @summary 获取用户信息接口
     * @request GET:/api/account/profile
     */
    accountProfile: (params: RequestParams = {}) =>
      this.request<ProfileUserInfoModel, RequestResponse>({
        path: `/api/account/profile`,
        method: 'GET',
        format: 'json',
        ...params,
      }),
    /**
     * @description 使用此接口获取用户信息，需要User权限
     *
     * @tags Account
     * @name AccountProfile
     * @summary 获取用户信息接口
     * @request GET:/api/account/profile
     */
    useAccountProfile: (options?: SWRConfiguration, doFetch: boolean = true) =>
      useSWR<ProfileUserInfoModel, RequestResponse>(
        doFetch ? `/api/account/profile` : null,
        options
      ),

    /**
     * @description 使用此接口获取用户信息，需要User权限
     *
     * @tags Account
     * @name AccountProfile
     * @summary 获取用户信息接口
     * @request GET:/api/account/profile
     */
    mutateAccountProfile: (
      data?: ProfileUserInfoModel | Promise<ProfileUserInfoModel>,
      options?: MutatorOptions
    ) => mutate<ProfileUserInfoModel>(`/api/account/profile`, data, options),

    /**
     * @description 使用此接口更新用户头像，需要User权限
     *
     * @tags Account
     * @name AccountAvatar
     * @summary 更新用户头像接口
     * @request PUT:/api/account/avatar
     */
    accountAvatar: (data: { file?: File }, params: RequestParams = {}) =>
      this.request<string, RequestResponse>({
        path: `/api/account/avatar`,
        method: 'PUT',
        body: data,
        type: ContentType.FormData,
        format: 'json',
        ...params,
      }),
  }
  admin = {
    /**
     * @description 使用此接口获取全局设置，需要Admin权限
     *
     * @tags Admin
     * @name AdminGetConfigs
     * @summary 获取配置
     * @request GET:/api/admin/config
     */
    adminGetConfigs: (params: RequestParams = {}) =>
      this.request<ConfigEditModel, RequestResponse>({
        path: `/api/admin/config`,
        method: 'GET',
        format: 'json',
        ...params,
      }),
    /**
     * @description 使用此接口获取全局设置，需要Admin权限
     *
     * @tags Admin
     * @name AdminGetConfigs
     * @summary 获取配置
     * @request GET:/api/admin/config
     */
    useAdminGetConfigs: (options?: SWRConfiguration, doFetch: boolean = true) =>
      useSWR<ConfigEditModel, RequestResponse>(doFetch ? `/api/admin/config` : null, options),

    /**
     * @description 使用此接口获取全局设置，需要Admin权限
     *
     * @tags Admin
     * @name AdminGetConfigs
     * @summary 获取配置
     * @request GET:/api/admin/config
     */
    mutateAdminGetConfigs: (
      data?: ConfigEditModel | Promise<ConfigEditModel>,
      options?: MutatorOptions
    ) => mutate<ConfigEditModel>(`/api/admin/config`, data, options),

    /**
     * @description 使用此接口更改全局设置，需要Admin权限
     *
     * @tags Admin
     * @name AdminUpdateConfigs
     * @summary 更改配置
     * @request PUT:/api/admin/config
     */
    adminUpdateConfigs: (data: ConfigEditModel, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/admin/config`,
        method: 'PUT',
        body: data,
        type: ContentType.Json,
        ...params,
      }),

    /**
     * @description 使用此接口获取全部用户，需要Admin权限
     *
     * @tags Admin
     * @name AdminUsers
     * @summary 获取全部用户
     * @request GET:/api/admin/users
     */
    adminUsers: (query?: { count?: number; skip?: number }, params: RequestParams = {}) =>
      this.request<UserInfoModel[], RequestResponse>({
        path: `/api/admin/users`,
        method: 'GET',
        query: query,
        format: 'json',
        ...params,
      }),
    /**
     * @description 使用此接口获取全部用户，需要Admin权限
     *
     * @tags Admin
     * @name AdminUsers
     * @summary 获取全部用户
     * @request GET:/api/admin/users
     */
    useAdminUsers: (
      query?: { count?: number; skip?: number },
      options?: SWRConfiguration,
      doFetch: boolean = true
    ) =>
      useSWR<UserInfoModel[], RequestResponse>(
        doFetch ? [`/api/admin/users`, query] : null,
        options
      ),

    /**
     * @description 使用此接口获取全部用户，需要Admin权限
     *
     * @tags Admin
     * @name AdminUsers
     * @summary 获取全部用户
     * @request GET:/api/admin/users
     */
    mutateAdminUsers: (
      query?: { count?: number; skip?: number },
      data?: UserInfoModel[] | Promise<UserInfoModel[]>,
      options?: MutatorOptions
    ) => mutate<UserInfoModel[]>([`/api/admin/users`, query], data, options),

    /**
     * @description 使用此接口搜索用户，需要Admin权限
     *
     * @tags Admin
     * @name AdminSearchUsers
     * @summary 搜索用户
     * @request POST:/api/admin/users/search
     */
    adminSearchUsers: (query?: { hint?: string }, params: RequestParams = {}) =>
      this.request<UserInfoModel[], RequestResponse>({
        path: `/api/admin/users/search`,
        method: 'POST',
        query: query,
        format: 'json',
        ...params,
      }),

    /**
     * @description 使用此接口获取全部队伍，需要Admin权限
     *
     * @tags Admin
     * @name AdminTeams
     * @summary 获取全部队伍信息
     * @request GET:/api/admin/teams
     */
    adminTeams: (query?: { count?: number; skip?: number }, params: RequestParams = {}) =>
      this.request<TeamInfoModel[], RequestResponse>({
        path: `/api/admin/teams`,
        method: 'GET',
        query: query,
        format: 'json',
        ...params,
      }),
    /**
     * @description 使用此接口获取全部队伍，需要Admin权限
     *
     * @tags Admin
     * @name AdminTeams
     * @summary 获取全部队伍信息
     * @request GET:/api/admin/teams
     */
    useAdminTeams: (
      query?: { count?: number; skip?: number },
      options?: SWRConfiguration,
      doFetch: boolean = true
    ) =>
      useSWR<TeamInfoModel[], RequestResponse>(
        doFetch ? [`/api/admin/teams`, query] : null,
        options
      ),

    /**
     * @description 使用此接口获取全部队伍，需要Admin权限
     *
     * @tags Admin
     * @name AdminTeams
     * @summary 获取全部队伍信息
     * @request GET:/api/admin/teams
     */
    mutateAdminTeams: (
      query?: { count?: number; skip?: number },
      data?: TeamInfoModel[] | Promise<TeamInfoModel[]>,
      options?: MutatorOptions
    ) => mutate<TeamInfoModel[]>([`/api/admin/teams`, query], data, options),

    /**
     * @description 使用此接口搜索队伍，需要Admin权限
     *
     * @tags Admin
     * @name AdminSearchTeams
     * @summary 搜索队伍
     * @request POST:/api/admin/teams/search
     */
    adminSearchTeams: (query?: { hint?: string }, params: RequestParams = {}) =>
      this.request<TeamInfoModel[], RequestResponse>({
        path: `/api/admin/teams/search`,
        method: 'POST',
        query: query,
        format: 'json',
        ...params,
      }),

    /**
     * @description 使用此接口修改用户信息，需要Admin权限
     *
     * @tags Admin
     * @name AdminUpdateUserInfo
     * @summary 修改用户信息
     * @request PUT:/api/admin/users/{userid}
     */
    adminUpdateUserInfo: (userid: string, data: UpdateUserInfoModel, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/admin/users/${userid}`,
        method: 'PUT',
        body: data,
        type: ContentType.Json,
        ...params,
      }),

    /**
     * @description 使用此接口删除用户，需要Admin权限
     *
     * @tags Admin
     * @name AdminDeleteUser
     * @summary 删除用户
     * @request DELETE:/api/admin/users/{userid}
     */
    adminDeleteUser: (userid: string, params: RequestParams = {}) =>
      this.request<string, RequestResponse>({
        path: `/api/admin/users/${userid}`,
        method: 'DELETE',
        format: 'json',
        ...params,
      }),

    /**
     * @description 使用此接口获取用户信息，需要Admin权限
     *
     * @tags Admin
     * @name AdminUserInfo
     * @summary 获取用户信息
     * @request GET:/api/admin/users/{userid}
     */
    adminUserInfo: (userid: string, params: RequestParams = {}) =>
      this.request<ProfileUserInfoModel, RequestResponse>({
        path: `/api/admin/users/${userid}`,
        method: 'GET',
        format: 'json',
        ...params,
      }),
    /**
     * @description 使用此接口获取用户信息，需要Admin权限
     *
     * @tags Admin
     * @name AdminUserInfo
     * @summary 获取用户信息
     * @request GET:/api/admin/users/{userid}
     */
    useAdminUserInfo: (userid: string, options?: SWRConfiguration, doFetch: boolean = true) =>
      useSWR<ProfileUserInfoModel, RequestResponse>(
        doFetch ? `/api/admin/users/${userid}` : null,
        options
      ),

    /**
     * @description 使用此接口获取用户信息，需要Admin权限
     *
     * @tags Admin
     * @name AdminUserInfo
     * @summary 获取用户信息
     * @request GET:/api/admin/users/{userid}
     */
    mutateAdminUserInfo: (
      userid: string,
      data?: ProfileUserInfoModel | Promise<ProfileUserInfoModel>,
      options?: MutatorOptions
    ) => mutate<ProfileUserInfoModel>(`/api/admin/users/${userid}`, data, options),

    /**
     * @description 使用此接口重置用户密码，需要Admin权限
     *
     * @tags Admin
     * @name AdminResetPassword
     * @summary 重置用户密码
     * @request DELETE:/api/admin/users/{userid}/password
     */
    adminResetPassword: (userid: string, params: RequestParams = {}) =>
      this.request<string, RequestResponse>({
        path: `/api/admin/users/${userid}/password`,
        method: 'DELETE',
        format: 'json',
        ...params,
      }),

    /**
     * @description 使用此接口删除队伍，需要Admin权限
     *
     * @tags Admin
     * @name AdminDeleteTeam
     * @summary 删除队伍
     * @request DELETE:/api/admin/teams/{id}
     */
    adminDeleteTeam: (id: number, params: RequestParams = {}) =>
      this.request<string, RequestResponse>({
        path: `/api/admin/teams/${id}`,
        method: 'DELETE',
        format: 'json',
        ...params,
      }),

    /**
     * @description 使用此接口获取全部日志，需要Admin权限
     *
     * @tags Admin
     * @name AdminLogs
     * @summary 获取全部日志
     * @request GET:/api/admin/logs
     */
    adminLogs: (
      query?: { level?: string | null; count?: number; skip?: number },
      params: RequestParams = {}
    ) =>
      this.request<LogMessageModel[], RequestResponse>({
        path: `/api/admin/logs`,
        method: 'GET',
        query: query,
        format: 'json',
        ...params,
      }),
    /**
     * @description 使用此接口获取全部日志，需要Admin权限
     *
     * @tags Admin
     * @name AdminLogs
     * @summary 获取全部日志
     * @request GET:/api/admin/logs
     */
    useAdminLogs: (
      query?: { level?: string | null; count?: number; skip?: number },
      options?: SWRConfiguration,
      doFetch: boolean = true
    ) =>
      useSWR<LogMessageModel[], RequestResponse>(
        doFetch ? [`/api/admin/logs`, query] : null,
        options
      ),

    /**
     * @description 使用此接口获取全部日志，需要Admin权限
     *
     * @tags Admin
     * @name AdminLogs
     * @summary 获取全部日志
     * @request GET:/api/admin/logs
     */
    mutateAdminLogs: (
      query?: { level?: string | null; count?: number; skip?: number },
      data?: LogMessageModel[] | Promise<LogMessageModel[]>,
      options?: MutatorOptions
    ) => mutate<LogMessageModel[]>([`/api/admin/logs`, query], data, options),

    /**
     * @description 使用此接口更新队伍参与状态，审核申请，需要Admin权限
     *
     * @tags Admin
     * @name AdminParticipation
     * @summary 更新参与状态
     * @request PUT:/api/admin/participation/{id}/{status}
     */
    adminParticipation: (id: number, status: ParticipationStatus, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/admin/participation/${id}/${status}`,
        method: 'PUT',
        ...params,
      }),

    /**
     * @description 使用此接口获取全部日志，需要Admin权限
     *
     * @tags Admin
     * @name AdminFiles
     * @summary 获取全部文件
     * @request GET:/api/admin/files
     */
    adminFiles: (query?: { count?: number; skip?: number }, params: RequestParams = {}) =>
      this.request<LocalFile[], RequestResponse>({
        path: `/api/admin/files`,
        method: 'GET',
        query: query,
        format: 'json',
        ...params,
      }),
    /**
     * @description 使用此接口获取全部日志，需要Admin权限
     *
     * @tags Admin
     * @name AdminFiles
     * @summary 获取全部文件
     * @request GET:/api/admin/files
     */
    useAdminFiles: (
      query?: { count?: number; skip?: number },
      options?: SWRConfiguration,
      doFetch: boolean = true
    ) =>
      useSWR<LocalFile[], RequestResponse>(doFetch ? [`/api/admin/files`, query] : null, options),

    /**
     * @description 使用此接口获取全部日志，需要Admin权限
     *
     * @tags Admin
     * @name AdminFiles
     * @summary 获取全部文件
     * @request GET:/api/admin/files
     */
    mutateAdminFiles: (
      query?: { count?: number; skip?: number },
      data?: LocalFile[] | Promise<LocalFile[]>,
      options?: MutatorOptions
    ) => mutate<LocalFile[]>([`/api/admin/files`, query], data, options),
  }
  assets = {
    /**
     * @description 根据哈希获取文件，不匹配文件名
     *
     * @tags Assets
     * @name AssetsGetFile
     * @summary 获取文件接口
     * @request GET:/assets/{hash}/{filename}
     */
    assetsGetFile: (hash: string, filename: string, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/assets/${hash}/${filename}`,
        method: 'GET',
        ...params,
      }),
    /**
     * @description 根据哈希获取文件，不匹配文件名
     *
     * @tags Assets
     * @name AssetsGetFile
     * @summary 获取文件接口
     * @request GET:/assets/{hash}/{filename}
     */
    useAssetsGetFile: (
      hash: string,
      filename: string,
      options?: SWRConfiguration,
      doFetch: boolean = true
    ) => useSWR<void, RequestResponse>(doFetch ? `/assets/${hash}/${filename}` : null, options),

    /**
     * @description 根据哈希获取文件，不匹配文件名
     *
     * @tags Assets
     * @name AssetsGetFile
     * @summary 获取文件接口
     * @request GET:/assets/{hash}/{filename}
     */
    mutateAssetsGetFile: (
      hash: string,
      filename: string,
      data?: void | Promise<void>,
      options?: MutatorOptions
    ) => mutate<void>(`/assets/${hash}/${filename}`, data, options),

    /**
     * @description 上传一个或多个文件
     *
     * @tags Assets
     * @name AssetsUpload
     * @summary 上传文件接口
     * @request POST:/api/assets
     */
    assetsUpload: (
      data: { files?: File[] },
      query?: { filename?: string | null },
      params: RequestParams = {}
    ) =>
      this.request<LocalFile[], RequestResponse>({
        path: `/api/assets`,
        method: 'POST',
        query: query,
        body: data,
        type: ContentType.FormData,
        format: 'json',
        ...params,
      }),

    /**
     * @description 按照文件哈希删除文件
     *
     * @tags Assets
     * @name AssetsDelete
     * @summary 删除文件接口
     * @request DELETE:/api/assets/{hash}
     */
    assetsDelete: (hash: string, params: RequestParams = {}) =>
      this.request<void, RequestResponse | ProblemDetails>({
        path: `/api/assets/${hash}`,
        method: 'DELETE',
        ...params,
      }),
  }
  edit = {
    /**
     * @description 添加文章，需要管理员权限
     *
     * @tags Edit
     * @name EditAddPost
     * @summary 添加文章
     * @request POST:/api/edit/posts
     */
    editAddPost: (data: PostEditModel, params: RequestParams = {}) =>
      this.request<string, RequestResponse>({
        path: `/api/edit/posts`,
        method: 'POST',
        body: data,
        type: ContentType.Json,
        format: 'json',
        ...params,
      }),

    /**
     * @description 修改文章，需要管理员权限
     *
     * @tags Edit
     * @name EditUpdatePost
     * @summary 修改文章
     * @request PUT:/api/edit/posts/{id}
     */
    editUpdatePost: (id: string, data: PostEditModel, params: RequestParams = {}) =>
      this.request<PostDetailModel, RequestResponse>({
        path: `/api/edit/posts/${id}`,
        method: 'PUT',
        body: data,
        type: ContentType.Json,
        format: 'json',
        ...params,
      }),

    /**
     * @description 删除文章，需要管理员权限
     *
     * @tags Edit
     * @name EditDeletePost
     * @summary 删除文章
     * @request DELETE:/api/edit/posts/{id}
     */
    editDeletePost: (id: string, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/edit/posts/${id}`,
        method: 'DELETE',
        ...params,
      }),

    /**
     * @description 添加比赛，需要管理员权限
     *
     * @tags Edit
     * @name EditAddGame
     * @summary 添加比赛
     * @request POST:/api/edit/games
     */
    editAddGame: (data: GameInfoModel, params: RequestParams = {}) =>
      this.request<GameInfoModel, RequestResponse>({
        path: `/api/edit/games`,
        method: 'POST',
        body: data,
        type: ContentType.Json,
        format: 'json',
        ...params,
      }),

    /**
     * @description 获取比赛列表，需要管理员权限
     *
     * @tags Edit
     * @name EditGetGames
     * @summary 获取比赛列表
     * @request GET:/api/edit/games
     */
    editGetGames: (query?: { count?: number; skip?: number }, params: RequestParams = {}) =>
      this.request<GameInfoModel[], RequestResponse>({
        path: `/api/edit/games`,
        method: 'GET',
        query: query,
        format: 'json',
        ...params,
      }),
    /**
     * @description 获取比赛列表，需要管理员权限
     *
     * @tags Edit
     * @name EditGetGames
     * @summary 获取比赛列表
     * @request GET:/api/edit/games
     */
    useEditGetGames: (
      query?: { count?: number; skip?: number },
      options?: SWRConfiguration,
      doFetch: boolean = true
    ) =>
      useSWR<GameInfoModel[], RequestResponse>(
        doFetch ? [`/api/edit/games`, query] : null,
        options
      ),

    /**
     * @description 获取比赛列表，需要管理员权限
     *
     * @tags Edit
     * @name EditGetGames
     * @summary 获取比赛列表
     * @request GET:/api/edit/games
     */
    mutateEditGetGames: (
      query?: { count?: number; skip?: number },
      data?: GameInfoModel[] | Promise<GameInfoModel[]>,
      options?: MutatorOptions
    ) => mutate<GameInfoModel[]>([`/api/edit/games`, query], data, options),

    /**
     * @description 获取比赛，需要管理员权限
     *
     * @tags Edit
     * @name EditGetGame
     * @summary 获取比赛
     * @request GET:/api/edit/games/{id}
     */
    editGetGame: (id: number, params: RequestParams = {}) =>
      this.request<GameInfoModel, RequestResponse>({
        path: `/api/edit/games/${id}`,
        method: 'GET',
        format: 'json',
        ...params,
      }),
    /**
     * @description 获取比赛，需要管理员权限
     *
     * @tags Edit
     * @name EditGetGame
     * @summary 获取比赛
     * @request GET:/api/edit/games/{id}
     */
    useEditGetGame: (id: number, options?: SWRConfiguration, doFetch: boolean = true) =>
      useSWR<GameInfoModel, RequestResponse>(doFetch ? `/api/edit/games/${id}` : null, options),

    /**
     * @description 获取比赛，需要管理员权限
     *
     * @tags Edit
     * @name EditGetGame
     * @summary 获取比赛
     * @request GET:/api/edit/games/{id}
     */
    mutateEditGetGame: (
      id: number,
      data?: GameInfoModel | Promise<GameInfoModel>,
      options?: MutatorOptions
    ) => mutate<GameInfoModel>(`/api/edit/games/${id}`, data, options),

    /**
     * @description 修改比赛，需要管理员权限
     *
     * @tags Edit
     * @name EditUpdateGame
     * @summary 修改比赛
     * @request PUT:/api/edit/games/{id}
     */
    editUpdateGame: (id: number, data: GameInfoModel, params: RequestParams = {}) =>
      this.request<GameInfoModel, RequestResponse>({
        path: `/api/edit/games/${id}`,
        method: 'PUT',
        body: data,
        type: ContentType.Json,
        format: 'json',
        ...params,
      }),

    /**
     * @description 删除比赛，需要管理员权限
     *
     * @tags Edit
     * @name EditDeleteGame
     * @summary 删除比赛
     * @request DELETE:/api/edit/games/{id}
     */
    editDeleteGame: (id: number, params: RequestParams = {}) =>
      this.request<GameInfoModel, RequestResponse>({
        path: `/api/edit/games/${id}`,
        method: 'DELETE',
        format: 'json',
        ...params,
      }),

    /**
     * @description 使用此接口更新比赛头图，需要Admin权限
     *
     * @tags Edit
     * @name EditUpdateGamePoster
     * @summary 更新比赛头图
     * @request PUT:/api/edit/games/{id}/poster
     */
    editUpdateGamePoster: (id: number, data: { file?: File }, params: RequestParams = {}) =>
      this.request<string, RequestResponse>({
        path: `/api/edit/games/${id}/poster`,
        method: 'PUT',
        body: data,
        type: ContentType.FormData,
        format: 'json',
        ...params,
      }),

    /**
     * @description 添加比赛文章，需要管理员权限
     *
     * @tags Edit
     * @name EditAddGameNotice
     * @summary 添加比赛文章
     * @request POST:/api/edit/games/{id}/posts
     */
    editAddGameNotice: (id: number, data: GameNoticeModel, params: RequestParams = {}) =>
      this.request<GameNotice, RequestResponse>({
        path: `/api/edit/games/${id}/posts`,
        method: 'POST',
        body: data,
        type: ContentType.Json,
        format: 'json',
        ...params,
      }),

    /**
     * @description 获取比赛文章，需要管理员权限
     *
     * @tags Edit
     * @name EditGetGameNotices
     * @summary 获取比赛文章
     * @request GET:/api/edit/games/{id}/posts
     */
    editGetGameNotices: (id: number, params: RequestParams = {}) =>
      this.request<GameNotice[], RequestResponse>({
        path: `/api/edit/games/${id}/posts`,
        method: 'GET',
        format: 'json',
        ...params,
      }),
    /**
     * @description 获取比赛文章，需要管理员权限
     *
     * @tags Edit
     * @name EditGetGameNotices
     * @summary 获取比赛文章
     * @request GET:/api/edit/games/{id}/posts
     */
    useEditGetGameNotices: (id: number, options?: SWRConfiguration, doFetch: boolean = true) =>
      useSWR<GameNotice[], RequestResponse>(
        doFetch ? `/api/edit/games/${id}/posts` : null,
        options
      ),

    /**
     * @description 获取比赛文章，需要管理员权限
     *
     * @tags Edit
     * @name EditGetGameNotices
     * @summary 获取比赛文章
     * @request GET:/api/edit/games/{id}/posts
     */
    mutateEditGetGameNotices: (
      id: number,
      data?: GameNotice[] | Promise<GameNotice[]>,
      options?: MutatorOptions
    ) => mutate<GameNotice[]>(`/api/edit/games/${id}/posts`, data, options),

    /**
     * @description 更新比赛文章，需要管理员权限
     *
     * @tags Edit
     * @name EditUpdateGameNotice
     * @summary 更新比赛文章
     * @request PUT:/api/edit/games/{id}/posts/{noticeId}
     */
    editUpdateGameNotice: (
      id: number,
      noticeId: number,
      data: GameNoticeModel,
      params: RequestParams = {}
    ) =>
      this.request<GameNotice, RequestResponse>({
        path: `/api/edit/games/${id}/posts/${noticeId}`,
        method: 'PUT',
        body: data,
        type: ContentType.Json,
        format: 'json',
        ...params,
      }),

    /**
     * @description 删除比赛文章，需要管理员权限
     *
     * @tags Edit
     * @name EditDeleteGameNotice
     * @summary 删除比赛文章
     * @request DELETE:/api/edit/games/{id}/posts/{noticeId}
     */
    editDeleteGameNotice: (id: number, noticeId: number, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/edit/games/${id}/posts/${noticeId}`,
        method: 'DELETE',
        ...params,
      }),

    /**
     * @description 添加比赛题目，需要管理员权限
     *
     * @tags Edit
     * @name EditAddGameChallenge
     * @summary 添加比赛题目
     * @request POST:/api/edit/games/{id}/challenges
     */
    editAddGameChallenge: (id: number, data: ChallengeInfoModel, params: RequestParams = {}) =>
      this.request<ChallengeEditDetailModel, RequestResponse>({
        path: `/api/edit/games/${id}/challenges`,
        method: 'POST',
        body: data,
        type: ContentType.Json,
        format: 'json',
        ...params,
      }),

    /**
     * @description 获取全部比赛题目，需要管理员权限
     *
     * @tags Edit
     * @name EditGetGameChallenges
     * @summary 获取全部比赛题目
     * @request GET:/api/edit/games/{id}/challenges
     */
    editGetGameChallenges: (id: number, params: RequestParams = {}) =>
      this.request<ChallengeInfoModel[], RequestResponse>({
        path: `/api/edit/games/${id}/challenges`,
        method: 'GET',
        format: 'json',
        ...params,
      }),
    /**
     * @description 获取全部比赛题目，需要管理员权限
     *
     * @tags Edit
     * @name EditGetGameChallenges
     * @summary 获取全部比赛题目
     * @request GET:/api/edit/games/{id}/challenges
     */
    useEditGetGameChallenges: (id: number, options?: SWRConfiguration, doFetch: boolean = true) =>
      useSWR<ChallengeInfoModel[], RequestResponse>(
        doFetch ? `/api/edit/games/${id}/challenges` : null,
        options
      ),

    /**
     * @description 获取全部比赛题目，需要管理员权限
     *
     * @tags Edit
     * @name EditGetGameChallenges
     * @summary 获取全部比赛题目
     * @request GET:/api/edit/games/{id}/challenges
     */
    mutateEditGetGameChallenges: (
      id: number,
      data?: ChallengeInfoModel[] | Promise<ChallengeInfoModel[]>,
      options?: MutatorOptions
    ) => mutate<ChallengeInfoModel[]>(`/api/edit/games/${id}/challenges`, data, options),

    /**
     * @description 获取比赛题目，需要管理员权限
     *
     * @tags Edit
     * @name EditGetGameChallenge
     * @summary 获取比赛题目
     * @request GET:/api/edit/games/{id}/challenges/{cId}
     */
    editGetGameChallenge: (id: number, cId: number, params: RequestParams = {}) =>
      this.request<ChallengeEditDetailModel, RequestResponse>({
        path: `/api/edit/games/${id}/challenges/${cId}`,
        method: 'GET',
        format: 'json',
        ...params,
      }),
    /**
     * @description 获取比赛题目，需要管理员权限
     *
     * @tags Edit
     * @name EditGetGameChallenge
     * @summary 获取比赛题目
     * @request GET:/api/edit/games/{id}/challenges/{cId}
     */
    useEditGetGameChallenge: (
      id: number,
      cId: number,
      options?: SWRConfiguration,
      doFetch: boolean = true
    ) =>
      useSWR<ChallengeEditDetailModel, RequestResponse>(
        doFetch ? `/api/edit/games/${id}/challenges/${cId}` : null,
        options
      ),

    /**
     * @description 获取比赛题目，需要管理员权限
     *
     * @tags Edit
     * @name EditGetGameChallenge
     * @summary 获取比赛题目
     * @request GET:/api/edit/games/{id}/challenges/{cId}
     */
    mutateEditGetGameChallenge: (
      id: number,
      cId: number,
      data?: ChallengeEditDetailModel | Promise<ChallengeEditDetailModel>,
      options?: MutatorOptions
    ) => mutate<ChallengeEditDetailModel>(`/api/edit/games/${id}/challenges/${cId}`, data, options),

    /**
     * @description 修改比赛题目，需要管理员权限
     *
     * @tags Edit
     * @name EditUpdateGameChallenge
     * @summary 修改比赛题目信息，Flags 不受更改，使用 Flag 相关 API 修改
     * @request PUT:/api/edit/games/{id}/challenges/{cId}
     */
    editUpdateGameChallenge: (
      id: number,
      cId: number,
      data: ChallengeUpdateModel,
      params: RequestParams = {}
    ) =>
      this.request<ChallengeEditDetailModel, RequestResponse>({
        path: `/api/edit/games/${id}/challenges/${cId}`,
        method: 'PUT',
        body: data,
        type: ContentType.Json,
        format: 'json',
        ...params,
      }),

    /**
     * @description 删除比赛题目，需要管理员权限
     *
     * @tags Edit
     * @name EditRemoveGameChallenge
     * @summary 删除比赛题目
     * @request DELETE:/api/edit/games/{id}/challenges/{cId}
     */
    editRemoveGameChallenge: (id: number, cId: number, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/edit/games/${id}/challenges/${cId}`,
        method: 'DELETE',
        ...params,
      }),

    /**
     * @description 测试比赛题目容器，需要管理员权限
     *
     * @tags Edit
     * @name EditCreateTestContainer
     * @summary 测试比赛题目容器
     * @request POST:/api/edit/games/{id}/challenges/{cId}/container
     */
    editCreateTestContainer: (id: number, cId: number, params: RequestParams = {}) =>
      this.request<ContainerInfoModel, RequestResponse>({
        path: `/api/edit/games/${id}/challenges/${cId}/container`,
        method: 'POST',
        format: 'json',
        ...params,
      }),

    /**
     * @description 关闭测试比赛题目容器，需要管理员权限
     *
     * @tags Edit
     * @name EditDestroyTestContainer
     * @summary 关闭测试比赛题目容器
     * @request DELETE:/api/edit/games/{id}/challenges/{cId}/container
     */
    editDestroyTestContainer: (id: number, cId: number, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/edit/games/${id}/challenges/${cId}/container`,
        method: 'DELETE',
        ...params,
      }),

    /**
     * @description 更新比赛题目附件，需要管理员权限，仅用于非动态附件题目
     *
     * @tags Edit
     * @name EditUpdateAttachment
     * @summary 更新比赛题目附件
     * @request POST:/api/edit/games/{id}/challenges/{cId}/attachment
     */
    editUpdateAttachment: (
      id: number,
      cId: number,
      data: AttachmentCreateModel,
      params: RequestParams = {}
    ) =>
      this.request<number, RequestResponse>({
        path: `/api/edit/games/${id}/challenges/${cId}/attachment`,
        method: 'POST',
        body: data,
        type: ContentType.Json,
        format: 'json',
        ...params,
      }),

    /**
     * @description 添加比赛题目 Flag，需要管理员权限
     *
     * @tags Edit
     * @name EditAddFlags
     * @summary 添加比赛题目 Flag
     * @request POST:/api/edit/games/{id}/challenges/{cId}/flags
     */
    editAddFlags: (id: number, cId: number, data: FlagCreateModel[], params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/edit/games/${id}/challenges/${cId}/flags`,
        method: 'POST',
        body: data,
        type: ContentType.Json,
        ...params,
      }),

    /**
     * @description 删除比赛题目 Flag，需要管理员权限
     *
     * @tags Edit
     * @name EditRemoveFlag
     * @summary 删除比赛题目 Flag
     * @request DELETE:/api/edit/games/{id}/challenges/{cId}/flags/{fId}
     */
    editRemoveFlag: (id: number, cId: number, fId: number, params: RequestParams = {}) =>
      this.request<TaskStatus, RequestResponse>({
        path: `/api/edit/games/${id}/challenges/${cId}/flags/${fId}`,
        method: 'DELETE',
        format: 'json',
        ...params,
      }),
  }
  game = {
    /**
     * @description 获取最近十个比赛
     *
     * @tags Game
     * @name GameGamesAll
     * @summary 获取最新的比赛
     * @request GET:/api/game
     */
    gameGamesAll: (params: RequestParams = {}) =>
      this.request<BasicGameInfoModel[], RequestResponse>({
        path: `/api/game`,
        method: 'GET',
        format: 'json',
        ...params,
      }),
    /**
     * @description 获取最近十个比赛
     *
     * @tags Game
     * @name GameGamesAll
     * @summary 获取最新的比赛
     * @request GET:/api/game
     */
    useGameGamesAll: (options?: SWRConfiguration, doFetch: boolean = true) =>
      useSWR<BasicGameInfoModel[], RequestResponse>(doFetch ? `/api/game` : null, options),

    /**
     * @description 获取最近十个比赛
     *
     * @tags Game
     * @name GameGamesAll
     * @summary 获取最新的比赛
     * @request GET:/api/game
     */
    mutateGameGamesAll: (
      data?: BasicGameInfoModel[] | Promise<BasicGameInfoModel[]>,
      options?: MutatorOptions
    ) => mutate<BasicGameInfoModel[]>(`/api/game`, data, options),

    /**
     * @description 获取比赛的详细信息
     *
     * @tags Game
     * @name GameGames
     * @summary 获取比赛详细信息
     * @request GET:/api/game/{id}
     */
    gameGames: (id: number, params: RequestParams = {}) =>
      this.request<GameDetailModel, RequestResponse>({
        path: `/api/game/${id}`,
        method: 'GET',
        format: 'json',
        ...params,
      }),
    /**
     * @description 获取比赛的详细信息
     *
     * @tags Game
     * @name GameGames
     * @summary 获取比赛详细信息
     * @request GET:/api/game/{id}
     */
    useGameGames: (id: number, options?: SWRConfiguration, doFetch: boolean = true) =>
      useSWR<GameDetailModel, RequestResponse>(doFetch ? `/api/game/${id}` : null, options),

    /**
     * @description 获取比赛的详细信息
     *
     * @tags Game
     * @name GameGames
     * @summary 获取比赛详细信息
     * @request GET:/api/game/{id}
     */
    mutateGameGames: (
      id: number,
      data?: GameDetailModel | Promise<GameDetailModel>,
      options?: MutatorOptions
    ) => mutate<GameDetailModel>(`/api/game/${id}`, data, options),

    /**
     * @description 加入一场比赛，需要User权限
     *
     * @tags Game
     * @name GameJoinGame
     * @summary 加入一个比赛
     * @request POST:/api/game/{id}
     */
    gameJoinGame: (id: number, data: GameJoinModel, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/game/${id}`,
        method: 'POST',
        body: data,
        type: ContentType.Json,
        ...params,
      }),

    /**
     * @description 退出一场比赛，需要User权限
     *
     * @tags Game
     * @name GameLeaveGame
     * @summary 退出一个比赛
     * @request DELETE:/api/game/{id}
     */
    gameLeaveGame: (id: number, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/game/${id}`,
        method: 'DELETE',
        ...params,
      }),

    /**
     * @description 获取积分榜数据
     *
     * @tags Game
     * @name GameScoreboard
     * @summary 获取积分榜
     * @request GET:/api/game/{id}/scoreboard
     */
    gameScoreboard: (id: number, params: RequestParams = {}) =>
      this.request<ScoreboardModel, RequestResponse>({
        path: `/api/game/${id}/scoreboard`,
        method: 'GET',
        format: 'json',
        ...params,
      }),
    /**
     * @description 获取积分榜数据
     *
     * @tags Game
     * @name GameScoreboard
     * @summary 获取积分榜
     * @request GET:/api/game/{id}/scoreboard
     */
    useGameScoreboard: (id: number, options?: SWRConfiguration, doFetch: boolean = true) =>
      useSWR<ScoreboardModel, RequestResponse>(
        doFetch ? `/api/game/${id}/scoreboard` : null,
        options
      ),

    /**
     * @description 获取积分榜数据
     *
     * @tags Game
     * @name GameScoreboard
     * @summary 获取积分榜
     * @request GET:/api/game/{id}/scoreboard
     */
    mutateGameScoreboard: (
      id: number,
      data?: ScoreboardModel | Promise<ScoreboardModel>,
      options?: MutatorOptions
    ) => mutate<ScoreboardModel>(`/api/game/${id}/scoreboard`, data, options),

    /**
     * @description 获取比赛通知数据
     *
     * @tags Game
     * @name GameNotices
     * @summary 获取比赛通知
     * @request GET:/api/game/{id}/notices
     */
    gameNotices: (
      id: number,
      query?: { count?: number; skip?: number },
      params: RequestParams = {}
    ) =>
      this.request<GameNotice[], RequestResponse>({
        path: `/api/game/${id}/notices`,
        method: 'GET',
        query: query,
        format: 'json',
        ...params,
      }),
    /**
     * @description 获取比赛通知数据
     *
     * @tags Game
     * @name GameNotices
     * @summary 获取比赛通知
     * @request GET:/api/game/{id}/notices
     */
    useGameNotices: (
      id: number,
      query?: { count?: number; skip?: number },
      options?: SWRConfiguration,
      doFetch: boolean = true
    ) =>
      useSWR<GameNotice[], RequestResponse>(
        doFetch ? [`/api/game/${id}/notices`, query] : null,
        options
      ),

    /**
     * @description 获取比赛通知数据
     *
     * @tags Game
     * @name GameNotices
     * @summary 获取比赛通知
     * @request GET:/api/game/{id}/notices
     */
    mutateGameNotices: (
      id: number,
      query?: { count?: number; skip?: number },
      data?: GameNotice[] | Promise<GameNotice[]>,
      options?: MutatorOptions
    ) => mutate<GameNotice[]>([`/api/game/${id}/notices`, query], data, options),

    /**
     * @description 获取比赛事件数据，需要Monitor权限
     *
     * @tags Game
     * @name GameEvents
     * @summary 获取比赛事件
     * @request GET:/api/game/{id}/events
     */
    gameEvents: (
      id: number,
      query?: { hideContainer?: boolean; count?: number; skip?: number },
      params: RequestParams = {}
    ) =>
      this.request<GameEvent[], RequestResponse>({
        path: `/api/game/${id}/events`,
        method: 'GET',
        query: query,
        format: 'json',
        ...params,
      }),
    /**
     * @description 获取比赛事件数据，需要Monitor权限
     *
     * @tags Game
     * @name GameEvents
     * @summary 获取比赛事件
     * @request GET:/api/game/{id}/events
     */
    useGameEvents: (
      id: number,
      query?: { hideContainer?: boolean; count?: number; skip?: number },
      options?: SWRConfiguration,
      doFetch: boolean = true
    ) =>
      useSWR<GameEvent[], RequestResponse>(
        doFetch ? [`/api/game/${id}/events`, query] : null,
        options
      ),

    /**
     * @description 获取比赛事件数据，需要Monitor权限
     *
     * @tags Game
     * @name GameEvents
     * @summary 获取比赛事件
     * @request GET:/api/game/{id}/events
     */
    mutateGameEvents: (
      id: number,
      query?: { hideContainer?: boolean; count?: number; skip?: number },
      data?: GameEvent[] | Promise<GameEvent[]>,
      options?: MutatorOptions
    ) => mutate<GameEvent[]>([`/api/game/${id}/events`, query], data, options),

    /**
     * @description 获取比赛提交数据，需要Monitor权限
     *
     * @tags Game
     * @name GameSubmissions
     * @summary 获取比赛提交
     * @request GET:/api/game/{id}/submissions
     */
    gameSubmissions: (
      id: number,
      query?: { type?: AnswerResult | null; count?: number; skip?: number },
      params: RequestParams = {}
    ) =>
      this.request<Submission[], RequestResponse>({
        path: `/api/game/${id}/submissions`,
        method: 'GET',
        query: query,
        format: 'json',
        ...params,
      }),
    /**
     * @description 获取比赛提交数据，需要Monitor权限
     *
     * @tags Game
     * @name GameSubmissions
     * @summary 获取比赛提交
     * @request GET:/api/game/{id}/submissions
     */
    useGameSubmissions: (
      id: number,
      query?: { type?: AnswerResult | null; count?: number; skip?: number },
      options?: SWRConfiguration,
      doFetch: boolean = true
    ) =>
      useSWR<Submission[], RequestResponse>(
        doFetch ? [`/api/game/${id}/submissions`, query] : null,
        options
      ),

    /**
     * @description 获取比赛提交数据，需要Monitor权限
     *
     * @tags Game
     * @name GameSubmissions
     * @summary 获取比赛提交
     * @request GET:/api/game/{id}/submissions
     */
    mutateGameSubmissions: (
      id: number,
      query?: { type?: AnswerResult | null; count?: number; skip?: number },
      data?: Submission[] | Promise<Submission[]>,
      options?: MutatorOptions
    ) => mutate<Submission[]>([`/api/game/${id}/submissions`, query], data, options),

    /**
     * @description 获取比赛的全部题目，需要User权限，需要当前激活队伍已经报名
     *
     * @tags Game
     * @name GameChallenges
     * @summary 获取全部比赛题目信息
     * @request GET:/api/game/{id}/challenges
     */
    gameChallenges: (id: number, params: RequestParams = {}) =>
      this.request<Record<string, ChallengeInfo[]>, RequestResponse>({
        path: `/api/game/${id}/challenges`,
        method: 'GET',
        format: 'json',
        ...params,
      }),
    /**
     * @description 获取比赛的全部题目，需要User权限，需要当前激活队伍已经报名
     *
     * @tags Game
     * @name GameChallenges
     * @summary 获取全部比赛题目信息
     * @request GET:/api/game/{id}/challenges
     */
    useGameChallenges: (id: number, options?: SWRConfiguration, doFetch: boolean = true) =>
      useSWR<Record<string, ChallengeInfo[]>, RequestResponse>(
        doFetch ? `/api/game/${id}/challenges` : null,
        options
      ),

    /**
     * @description 获取比赛的全部题目，需要User权限，需要当前激活队伍已经报名
     *
     * @tags Game
     * @name GameChallenges
     * @summary 获取全部比赛题目信息
     * @request GET:/api/game/{id}/challenges
     */
    mutateGameChallenges: (
      id: number,
      data?: Record<string, ChallengeInfo[]> | Promise<Record<string, ChallengeInfo[]>>,
      options?: MutatorOptions
    ) => mutate<Record<string, ChallengeInfo[]>>(`/api/game/${id}/challenges`, data, options),

    /**
     * @description 获取当前队伍的比赛信息，需要User权限，需要当前激活队伍已经报名
     *
     * @tags Game
     * @name GameMyTeam
     * @summary 获取当前队伍比赛信息
     * @request GET:/api/game/{id}/myteam
     */
    gameMyTeam: (id: number, params: RequestParams = {}) =>
      this.request<GameTeamDetailModel, RequestResponse>({
        path: `/api/game/${id}/myteam`,
        method: 'GET',
        format: 'json',
        ...params,
      }),
    /**
     * @description 获取当前队伍的比赛信息，需要User权限，需要当前激活队伍已经报名
     *
     * @tags Game
     * @name GameMyTeam
     * @summary 获取当前队伍比赛信息
     * @request GET:/api/game/{id}/myteam
     */
    useGameMyTeam: (id: number, options?: SWRConfiguration, doFetch: boolean = true) =>
      useSWR<GameTeamDetailModel, RequestResponse>(
        doFetch ? `/api/game/${id}/myteam` : null,
        options
      ),

    /**
     * @description 获取当前队伍的比赛信息，需要User权限，需要当前激活队伍已经报名
     *
     * @tags Game
     * @name GameMyTeam
     * @summary 获取当前队伍比赛信息
     * @request GET:/api/game/{id}/myteam
     */
    mutateGameMyTeam: (
      id: number,
      data?: GameTeamDetailModel | Promise<GameTeamDetailModel>,
      options?: MutatorOptions
    ) => mutate<GameTeamDetailModel>(`/api/game/${id}/myteam`, data, options),

    /**
     * @description 获取比赛的全部题目参与信息，需要Admin权限
     *
     * @tags Game
     * @name GameParticipations
     * @summary 获取全部比赛参与信息
     * @request GET:/api/game/{id}/participations
     */
    gameParticipations: (id: number, params: RequestParams = {}) =>
      this.request<ParticipationInfoModel[], RequestResponse>({
        path: `/api/game/${id}/participations`,
        method: 'GET',
        format: 'json',
        ...params,
      }),
    /**
     * @description 获取比赛的全部题目参与信息，需要Admin权限
     *
     * @tags Game
     * @name GameParticipations
     * @summary 获取全部比赛参与信息
     * @request GET:/api/game/{id}/participations
     */
    useGameParticipations: (id: number, options?: SWRConfiguration, doFetch: boolean = true) =>
      useSWR<ParticipationInfoModel[], RequestResponse>(
        doFetch ? `/api/game/${id}/participations` : null,
        options
      ),

    /**
     * @description 获取比赛的全部题目参与信息，需要Admin权限
     *
     * @tags Game
     * @name GameParticipations
     * @summary 获取全部比赛参与信息
     * @request GET:/api/game/{id}/participations
     */
    mutateGameParticipations: (
      id: number,
      data?: ParticipationInfoModel[] | Promise<ParticipationInfoModel[]>,
      options?: MutatorOptions
    ) => mutate<ParticipationInfoModel[]>(`/api/game/${id}/participations`, data, options),

    /**
     * @description 下载比赛积分榜，需要Monitor权限
     *
     * @tags Game
     * @name GameScoreboardSheet
     * @summary 下载比赛积分榜
     * @request GET:/api/game/{id}/scoreboardsheet
     */
    gameScoreboardSheet: (id: number, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/game/${id}/scoreboardsheet`,
        method: 'GET',
        ...params,
      }),
    /**
     * @description 下载比赛积分榜，需要Monitor权限
     *
     * @tags Game
     * @name GameScoreboardSheet
     * @summary 下载比赛积分榜
     * @request GET:/api/game/{id}/scoreboardsheet
     */
    useGameScoreboardSheet: (id: number, options?: SWRConfiguration, doFetch: boolean = true) =>
      useSWR<void, RequestResponse>(doFetch ? `/api/game/${id}/scoreboardsheet` : null, options),

    /**
     * @description 下载比赛积分榜，需要Monitor权限
     *
     * @tags Game
     * @name GameScoreboardSheet
     * @summary 下载比赛积分榜
     * @request GET:/api/game/{id}/scoreboardsheet
     */
    mutateGameScoreboardSheet: (
      id: number,
      data?: void | Promise<void>,
      options?: MutatorOptions
    ) => mutate<void>(`/api/game/${id}/scoreboardsheet`, data, options),

    /**
     * @description 获取比赛题目信息，需要User权限，需要当前激活队伍已经报名
     *
     * @tags Game
     * @name GameGetChallenge
     * @summary 获取比赛题目信息
     * @request GET:/api/game/{id}/challenges/{challengeId}
     */
    gameGetChallenge: (id: number, challengeId: number, params: RequestParams = {}) =>
      this.request<ChallengeDetailModel, RequestResponse>({
        path: `/api/game/${id}/challenges/${challengeId}`,
        method: 'GET',
        format: 'json',
        ...params,
      }),
    /**
     * @description 获取比赛题目信息，需要User权限，需要当前激活队伍已经报名
     *
     * @tags Game
     * @name GameGetChallenge
     * @summary 获取比赛题目信息
     * @request GET:/api/game/{id}/challenges/{challengeId}
     */
    useGameGetChallenge: (
      id: number,
      challengeId: number,
      options?: SWRConfiguration,
      doFetch: boolean = true
    ) =>
      useSWR<ChallengeDetailModel, RequestResponse>(
        doFetch ? `/api/game/${id}/challenges/${challengeId}` : null,
        options
      ),

    /**
     * @description 获取比赛题目信息，需要User权限，需要当前激活队伍已经报名
     *
     * @tags Game
     * @name GameGetChallenge
     * @summary 获取比赛题目信息
     * @request GET:/api/game/{id}/challenges/{challengeId}
     */
    mutateGameGetChallenge: (
      id: number,
      challengeId: number,
      data?: ChallengeDetailModel | Promise<ChallengeDetailModel>,
      options?: MutatorOptions
    ) => mutate<ChallengeDetailModel>(`/api/game/${id}/challenges/${challengeId}`, data, options),

    /**
     * @description 提交 flag，需要User权限，需要当前激活队伍已经报名
     *
     * @tags Game
     * @name GameSubmit
     * @summary 提交 flag
     * @request POST:/api/game/{id}/challenges/{challengeId}
     */
    gameSubmit: (
      id: number,
      challengeId: number,
      data: FlagSubmitModel,
      params: RequestParams = {}
    ) =>
      this.request<number, RequestResponse>({
        path: `/api/game/${id}/challenges/${challengeId}`,
        method: 'POST',
        body: data,
        type: ContentType.Json,
        format: 'json',
        ...params,
      }),

    /**
     * @description 查询 flag 状态，需要User权限
     *
     * @tags Game
     * @name GameStatus
     * @summary 查询 flag 状态
     * @request GET:/api/game/{id}/challenges/{challengeId}/status/{submitId}
     */
    gameStatus: (id: number, challengeId: number, submitId: number, params: RequestParams = {}) =>
      this.request<AnswerResult, RequestResponse>({
        path: `/api/game/${id}/challenges/${challengeId}/status/${submitId}`,
        method: 'GET',
        format: 'json',
        ...params,
      }),
    /**
     * @description 查询 flag 状态，需要User权限
     *
     * @tags Game
     * @name GameStatus
     * @summary 查询 flag 状态
     * @request GET:/api/game/{id}/challenges/{challengeId}/status/{submitId}
     */
    useGameStatus: (
      id: number,
      challengeId: number,
      submitId: number,
      options?: SWRConfiguration,
      doFetch: boolean = true
    ) =>
      useSWR<AnswerResult, RequestResponse>(
        doFetch ? `/api/game/${id}/challenges/${challengeId}/status/${submitId}` : null,
        options
      ),

    /**
     * @description 查询 flag 状态，需要User权限
     *
     * @tags Game
     * @name GameStatus
     * @summary 查询 flag 状态
     * @request GET:/api/game/{id}/challenges/{challengeId}/status/{submitId}
     */
    mutateGameStatus: (
      id: number,
      challengeId: number,
      submitId: number,
      data?: AnswerResult | Promise<AnswerResult>,
      options?: MutatorOptions
    ) =>
      mutate<AnswerResult>(
        `/api/game/${id}/challenges/${challengeId}/status/${submitId}`,
        data,
        options
      ),

    /**
     * @description 创建容器，需要User权限
     *
     * @tags Game
     * @name GameCreateContainer
     * @summary 创建容器
     * @request POST:/api/game/{id}/container/{challengeId}
     */
    gameCreateContainer: (id: number, challengeId: number, params: RequestParams = {}) =>
      this.request<ContainerInfoModel, RequestResponse>({
        path: `/api/game/${id}/container/${challengeId}`,
        method: 'POST',
        format: 'json',
        ...params,
      }),

    /**
     * @description 删除，需要User权限
     *
     * @tags Game
     * @name GameDeleteContainer
     * @summary 删除容器
     * @request DELETE:/api/game/{id}/container/{challengeId}
     */
    gameDeleteContainer: (id: number, challengeId: number, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/game/${id}/container/${challengeId}`,
        method: 'DELETE',
        ...params,
      }),

    /**
     * @description 延长容器时间，需要User权限，且只能在到期前十分钟延期两小时
     *
     * @tags Game
     * @name GameProlongContainer
     * @summary 延长容器时间
     * @request POST:/api/game/{id}/container/{challengeId}/prolong
     */
    gameProlongContainer: (id: number, challengeId: number, params: RequestParams = {}) =>
      this.request<ContainerInfoModel, RequestResponse>({
        path: `/api/game/${id}/container/${challengeId}/prolong`,
        method: 'POST',
        format: 'json',
        ...params,
      }),
  }
  info = {
    /**
     * @description 获取最新文章
     *
     * @tags Info
     * @name InfoGetLatestPosts
     * @summary 获取最新文章
     * @request GET:/api/posts/latest
     */
    infoGetLatestPosts: (params: RequestParams = {}) =>
      this.request<PostInfoModel[], any>({
        path: `/api/posts/latest`,
        method: 'GET',
        format: 'json',
        ...params,
      }),
    /**
     * @description 获取最新文章
     *
     * @tags Info
     * @name InfoGetLatestPosts
     * @summary 获取最新文章
     * @request GET:/api/posts/latest
     */
    useInfoGetLatestPosts: (options?: SWRConfiguration, doFetch: boolean = true) =>
      useSWR<PostInfoModel[], any>(doFetch ? `/api/posts/latest` : null, options),

    /**
     * @description 获取最新文章
     *
     * @tags Info
     * @name InfoGetLatestPosts
     * @summary 获取最新文章
     * @request GET:/api/posts/latest
     */
    mutateInfoGetLatestPosts: (
      data?: PostInfoModel[] | Promise<PostInfoModel[]>,
      options?: MutatorOptions
    ) => mutate<PostInfoModel[]>(`/api/posts/latest`, data, options),

    /**
     * @description 获取全部文章
     *
     * @tags Info
     * @name InfoGetPosts
     * @summary 获取全部文章
     * @request GET:/api/posts
     */
    infoGetPosts: (params: RequestParams = {}) =>
      this.request<PostInfoModel[], any>({
        path: `/api/posts`,
        method: 'GET',
        format: 'json',
        ...params,
      }),
    /**
     * @description 获取全部文章
     *
     * @tags Info
     * @name InfoGetPosts
     * @summary 获取全部文章
     * @request GET:/api/posts
     */
    useInfoGetPosts: (options?: SWRConfiguration, doFetch: boolean = true) =>
      useSWR<PostInfoModel[], any>(doFetch ? `/api/posts` : null, options),

    /**
     * @description 获取全部文章
     *
     * @tags Info
     * @name InfoGetPosts
     * @summary 获取全部文章
     * @request GET:/api/posts
     */
    mutateInfoGetPosts: (
      data?: PostInfoModel[] | Promise<PostInfoModel[]>,
      options?: MutatorOptions
    ) => mutate<PostInfoModel[]>(`/api/posts`, data, options),

    /**
     * @description 获取文章详情
     *
     * @tags Info
     * @name InfoGetPost
     * @summary 获取文章详情
     * @request GET:/api/posts/{id}
     */
    infoGetPost: (id: string, params: RequestParams = {}) =>
      this.request<PostDetailModel, RequestResponse>({
        path: `/api/posts/${id}`,
        method: 'GET',
        format: 'json',
        ...params,
      }),
    /**
     * @description 获取文章详情
     *
     * @tags Info
     * @name InfoGetPost
     * @summary 获取文章详情
     * @request GET:/api/posts/{id}
     */
    useInfoGetPost: (id: string, options?: SWRConfiguration, doFetch: boolean = true) =>
      useSWR<PostDetailModel, RequestResponse>(doFetch ? `/api/posts/${id}` : null, options),

    /**
     * @description 获取文章详情
     *
     * @tags Info
     * @name InfoGetPost
     * @summary 获取文章详情
     * @request GET:/api/posts/{id}
     */
    mutateInfoGetPost: (
      id: string,
      data?: PostDetailModel | Promise<PostDetailModel>,
      options?: MutatorOptions
    ) => mutate<PostDetailModel>(`/api/posts/${id}`, data, options),

    /**
     * @description 获取全局设置
     *
     * @tags Info
     * @name InfoGetGlobalConfig
     * @summary 获取全局设置
     * @request GET:/api/config
     */
    infoGetGlobalConfig: (params: RequestParams = {}) =>
      this.request<GlobalConfig, any>({
        path: `/api/config`,
        method: 'GET',
        format: 'json',
        ...params,
      }),
    /**
     * @description 获取全局设置
     *
     * @tags Info
     * @name InfoGetGlobalConfig
     * @summary 获取全局设置
     * @request GET:/api/config
     */
    useInfoGetGlobalConfig: (options?: SWRConfiguration, doFetch: boolean = true) =>
      useSWR<GlobalConfig, any>(doFetch ? `/api/config` : null, options),

    /**
     * @description 获取全局设置
     *
     * @tags Info
     * @name InfoGetGlobalConfig
     * @summary 获取全局设置
     * @request GET:/api/config
     */
    mutateInfoGetGlobalConfig: (
      data?: GlobalConfig | Promise<GlobalConfig>,
      options?: MutatorOptions
    ) => mutate<GlobalConfig>(`/api/config`, data, options),

    /**
     * @description 获取 Recaptcha SiteKey
     *
     * @tags Info
     * @name InfoGetRecaptchaSiteKey
     * @summary 获取 Recaptcha SiteKey
     * @request GET:/api/sitekey
     */
    infoGetRecaptchaSiteKey: (params: RequestParams = {}) =>
      this.request<string, any>({
        path: `/api/sitekey`,
        method: 'GET',
        format: 'json',
        ...params,
      }),
    /**
     * @description 获取 Recaptcha SiteKey
     *
     * @tags Info
     * @name InfoGetRecaptchaSiteKey
     * @summary 获取 Recaptcha SiteKey
     * @request GET:/api/sitekey
     */
    useInfoGetRecaptchaSiteKey: (options?: SWRConfiguration, doFetch: boolean = true) =>
      useSWR<string, any>(doFetch ? `/api/sitekey` : null, options),

    /**
     * @description 获取 Recaptcha SiteKey
     *
     * @tags Info
     * @name InfoGetRecaptchaSiteKey
     * @summary 获取 Recaptcha SiteKey
     * @request GET:/api/sitekey
     */
    mutateInfoGetRecaptchaSiteKey: (data?: string | Promise<string>, options?: MutatorOptions) =>
      mutate<string>(`/api/sitekey`, data, options),
  }
  team = {
    /**
     * @description 根据 id 获取一个队伍的基本信息
     *
     * @tags Team
     * @name TeamGetBasicInfo
     * @summary 获取队伍信息
     * @request GET:/api/team/{id}
     */
    teamGetBasicInfo: (id: number, params: RequestParams = {}) =>
      this.request<TeamInfoModel, RequestResponse>({
        path: `/api/team/${id}`,
        method: 'GET',
        format: 'json',
        ...params,
      }),
    /**
     * @description 根据 id 获取一个队伍的基本信息
     *
     * @tags Team
     * @name TeamGetBasicInfo
     * @summary 获取队伍信息
     * @request GET:/api/team/{id}
     */
    useTeamGetBasicInfo: (id: number, options?: SWRConfiguration, doFetch: boolean = true) =>
      useSWR<TeamInfoModel, RequestResponse>(doFetch ? `/api/team/${id}` : null, options),

    /**
     * @description 根据 id 获取一个队伍的基本信息
     *
     * @tags Team
     * @name TeamGetBasicInfo
     * @summary 获取队伍信息
     * @request GET:/api/team/{id}
     */
    mutateTeamGetBasicInfo: (
      id: number,
      data?: TeamInfoModel | Promise<TeamInfoModel>,
      options?: MutatorOptions
    ) => mutate<TeamInfoModel>(`/api/team/${id}`, data, options),

    /**
     * @description 队伍信息更改接口，需要为队伍创建者
     *
     * @tags Team
     * @name TeamUpdateTeam
     * @summary 更改队伍信息
     * @request PUT:/api/team/{id}
     */
    teamUpdateTeam: (id: number, data: TeamUpdateModel, params: RequestParams = {}) =>
      this.request<TeamInfoModel, RequestResponse>({
        path: `/api/team/${id}`,
        method: 'PUT',
        body: data,
        type: ContentType.Json,
        format: 'json',
        ...params,
      }),

    /**
     * @description 用户删除队伍接口，需要User权限，且为队伍队长
     *
     * @tags Team
     * @name TeamDeleteTeam
     * @summary 删除队伍
     * @request DELETE:/api/team/{id}
     */
    teamDeleteTeam: (id: number, params: RequestParams = {}) =>
      this.request<TeamInfoModel, RequestResponse>({
        path: `/api/team/${id}`,
        method: 'DELETE',
        format: 'json',
        ...params,
      }),

    /**
     * @description 根据用户获取一个队伍的基本信息
     *
     * @tags Team
     * @name TeamGetTeamsInfo
     * @summary 获取当前自己队伍信息
     * @request GET:/api/team
     */
    teamGetTeamsInfo: (params: RequestParams = {}) =>
      this.request<TeamInfoModel[], RequestResponse>({
        path: `/api/team`,
        method: 'GET',
        format: 'json',
        ...params,
      }),
    /**
     * @description 根据用户获取一个队伍的基本信息
     *
     * @tags Team
     * @name TeamGetTeamsInfo
     * @summary 获取当前自己队伍信息
     * @request GET:/api/team
     */
    useTeamGetTeamsInfo: (options?: SWRConfiguration, doFetch: boolean = true) =>
      useSWR<TeamInfoModel[], RequestResponse>(doFetch ? `/api/team` : null, options),

    /**
     * @description 根据用户获取一个队伍的基本信息
     *
     * @tags Team
     * @name TeamGetTeamsInfo
     * @summary 获取当前自己队伍信息
     * @request GET:/api/team
     */
    mutateTeamGetTeamsInfo: (
      data?: TeamInfoModel[] | Promise<TeamInfoModel[]>,
      options?: MutatorOptions
    ) => mutate<TeamInfoModel[]>(`/api/team`, data, options),

    /**
     * @description 用户创建队伍接口，每个用户只能创建一个队伍
     *
     * @tags Team
     * @name TeamCreateTeam
     * @summary 创建队伍
     * @request POST:/api/team
     */
    teamCreateTeam: (data: TeamUpdateModel, params: RequestParams = {}) =>
      this.request<TeamInfoModel, RequestResponse>({
        path: `/api/team`,
        method: 'POST',
        body: data,
        type: ContentType.Json,
        format: 'json',
        ...params,
      }),

    /**
     * @description 移交队伍所有权接口，需要为队伍创建者
     *
     * @tags Team
     * @name TeamTransfer
     * @summary 移交队伍所有权
     * @request PUT:/api/team/{id}/transfer
     */
    teamTransfer: (id: number, data: TeamTransferModel, params: RequestParams = {}) =>
      this.request<TeamInfoModel, RequestResponse>({
        path: `/api/team/${id}/transfer`,
        method: 'PUT',
        body: data,
        type: ContentType.Json,
        format: 'json',
        ...params,
      }),

    /**
     * @description 获取队伍邀请信息，需要为队伍创建者
     *
     * @tags Team
     * @name TeamInviteCode
     * @summary 获取邀请信息
     * @request GET:/api/team/{id}/invite
     */
    teamInviteCode: (id: number, params: RequestParams = {}) =>
      this.request<string, RequestResponse>({
        path: `/api/team/${id}/invite`,
        method: 'GET',
        format: 'json',
        ...params,
      }),
    /**
     * @description 获取队伍邀请信息，需要为队伍创建者
     *
     * @tags Team
     * @name TeamInviteCode
     * @summary 获取邀请信息
     * @request GET:/api/team/{id}/invite
     */
    useTeamInviteCode: (id: number, options?: SWRConfiguration, doFetch: boolean = true) =>
      useSWR<string, RequestResponse>(doFetch ? `/api/team/${id}/invite` : null, options),

    /**
     * @description 获取队伍邀请信息，需要为队伍创建者
     *
     * @tags Team
     * @name TeamInviteCode
     * @summary 获取邀请信息
     * @request GET:/api/team/{id}/invite
     */
    mutateTeamInviteCode: (id: number, data?: string | Promise<string>, options?: MutatorOptions) =>
      mutate<string>(`/api/team/${id}/invite`, data, options),

    /**
     * @description 更新邀请 Token 的接口，需要为队伍创建者
     *
     * @tags Team
     * @name TeamUpdateInviteToken
     * @summary 更新邀请 Token
     * @request PUT:/api/team/{id}/invite
     */
    teamUpdateInviteToken: (id: number, params: RequestParams = {}) =>
      this.request<string, RequestResponse>({
        path: `/api/team/${id}/invite`,
        method: 'PUT',
        format: 'json',
        ...params,
      }),

    /**
     * @description 踢除用户接口，踢出对应id的用户，需要队伍创建者权限
     *
     * @tags Team
     * @name TeamKickUser
     * @summary 踢除用户接口
     * @request POST:/api/team/{id}/kick/{userid}
     */
    teamKickUser: (id: number, userid: string, params: RequestParams = {}) =>
      this.request<TeamInfoModel, RequestResponse>({
        path: `/api/team/${id}/kick/${userid}`,
        method: 'POST',
        format: 'json',
        ...params,
      }),

    /**
     * @description 接受邀请的接口，需要User权限，且不在队伍中
     *
     * @tags Team
     * @name TeamAccept
     * @summary 接受邀请
     * @request POST:/api/team/accept
     */
    teamAccept: (data: string, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/team/accept`,
        method: 'POST',
        body: data,
        type: ContentType.Json,
        ...params,
      }),

    /**
     * @description 离开队伍的接口，需要User权限，且在队伍中
     *
     * @tags Team
     * @name TeamLeave
     * @summary 离开队伍
     * @request POST:/api/team/{id}/leave
     */
    teamLeave: (id: number, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/team/${id}/leave`,
        method: 'POST',
        ...params,
      }),

    /**
     * @description 使用此接口更新队伍头像，需要User权限，且为队伍成员
     *
     * @tags Team
     * @name TeamAvatar
     * @summary 更新队伍头像接口
     * @request PUT:/api/team/{id}/avatar
     */
    teamAvatar: (id: number, data: { file?: File }, params: RequestParams = {}) =>
      this.request<string, RequestResponse>({
        path: `/api/team/${id}/avatar`,
        method: 'PUT',
        body: data,
        type: ContentType.FormData,
        format: 'json',
        ...params,
      }),
  }
}

const api = new Api()
export default api
