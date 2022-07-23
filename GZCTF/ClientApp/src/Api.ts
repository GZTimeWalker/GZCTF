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

import useSWR, { mutate, MutatorOptions, SWRConfiguration } from 'swr'

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

  /**
   * 当前队伍
   * @format int32
   */
  activeTeamId?: number | null

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

  /** 所拥有的队伍 */
  ownTeamName?: string | null

  /** @format int32 */
  ownTeamId?: number | null

  /** 激活的队伍 */
  activeTeamName?: string | null

  /** @format int32 */
  activeTeamId?: number | null
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

export interface Notice {
  /** @format int32 */
  id?: number

  /** 通知标题 */
  title: string

  /** 通知内容 */
  content: string

  /** 是否置顶 */
  isPinned: boolean

  /**
   * 发布时间
   * @format date-time
   */
  time: string
}

/**
 * 全局通知（Edit）
 */
export interface NoticeModel {
  /** 通知标题 */
  title: string

  /** 通知内容 */
  content: string

  /** 是否置顶 */
  isPinned?: boolean
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

  /** 比赛描述 */
  summary?: string

  /** 比赛详细介绍 */
  content?: string

  /**
   * 队员数量限制, 0 为无上限
   * @format int32
   */
  teamMemberCountLimit?: number

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

export interface Game {
  /** @format int32 */
  id?: number

  /** 比赛标题 */
  title: string

  /** 头图哈希 */
  posterHash?: string | null

  /** 比赛描述 */
  summary?: string

  /** 比赛详细介绍 */
  content?: string

  /**
   * 队员数量限制, 0 为无上限
   * @format int32
   */
  teamMemberCountLimit?: number

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
  isActive?: boolean
  posterUrl?: string | null
}

/**
* 比赛通知，会发往客户端。
信息涵盖一二三血通知、提示发布通知、题目开启通知等
*/
export interface GameNotice {
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
  ErrorFix = 'ErrorFix',
}

/**
 * 比赛通知（Edit）
 */
export interface GameNoticeModel {
  /** 通知内容 */
  content: string
}

export interface Challenge {
  /** @format int32 */
  id?: number

  /** 题目名称 */
  title: string

  /** 题目内容 */
  content: string

  /** 是否启用题目 */
  isEnabled?: boolean

  /** 题目标签 */
  tag?: ChallengeTag

  /** 题目提示，用";"分隔 */
  hints?: string

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
   * @format int32
   */
  difficulty: number

  /** 题目类型 */
  type: ChallengeType

  /** 下载文件名称 */
  fileName?: string

  /**
   * 当前题目分值
   * @format int32
   */
  currentScore?: number
  flags?: FlagContext[]

  /** 提交 */
  submissions?: Submission[]

  /** 比赛对象 */
  game?: Game

  /** @format int32 */
  gameId?: number
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

export interface FlagContext {
  /** @format int32 */
  id?: number

  /** Flag 内容 */
  flag: string

  /** 附件类型 */
  attachmentType: FileType

  /** Flag 对应附件 (远程文件） */
  remoteUrl?: string | null

  /** Flag 对应文件（本地文件） */
  localFile?: LocalFile | null

  /** 是否已被占用 */
  isOccupied?: boolean

  /** 赛题 */
  challenge?: Challenge | null

  /**
   * 赛题Id
   * @format int32
   */
  challengeId?: number

  /** 附件访问链接 */
  url?: string | null
}

export enum FileType {
  None = 0,
  Local = 1,
  Remote = 2,
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

/**
 * 题目信息更改（Edit）
 */
export interface ChallengeModel {
  /** 题目名称 */
  title: string

  /** 题目内容 */
  content: string

  /** 题目标签 */
  tag?: ChallengeTag

  /** 题目提示，用";"分隔 */
  hints?: string

  /** 题目类型 */
  type?: ChallengeType

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
  originalScore?: number

  /**
   * 最低分数比例
   * @format double
   * @min 0
   * @max 1
   */
  minScoreRate: number

  /**
   * 难度系数
   * @format int32
   */
  difficulty: number

  /** 统一文件名 */
  fileName?: string | null
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
}

/**
 * Flag 信息（Edit）
 */
export interface FlagInfoModel {
  /** Flag文本 */
  flag?: string

  /** Flag 对应附件（本地文件哈希） */
  fileHash?: string | null

  /** Flag 对应附件 (远程文件） */
  url?: string | null
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
export interface GameDetailsModel {
  /** @format int32 */
  id?: number

  /** 比赛标题 */
  title?: string

  /** 比赛描述 */
  summary?: string

  /** 比赛详细介绍 */
  content?: string

  /** 比赛头图 */
  poster?: string | null

  /**
   * 队员数量限制
   * @format int32
   */
  limit?: number

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

/**
 * 排行榜
 */
export interface ScoreboardModel {
  /**
   * 更新时间
   * @format date-time
   */
  updateTimeUTC?: string

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
  rank?: SubmissionType
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

/**
 * 题目实例信息
 */
export interface InstanceInfoModel {
  /**
   * 实例 Id
   * @format int32
   */
  id?: number

  /**
   * 队伍 Id
   * @format int32
   */
  teamId?: number

  /** 队伍名 */
  teamName?: string

  /** 题目详情 */
  challenge?: ChallengeInfoModel

  /** 容器信息 */
  container?: ContainerInfoModel | null
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
  Destoryed = 'Destoryed',
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
  team?: TeamInfoModel

  /**
   * 队伍分值
   * @format int32
   */
  score?: number

  /** 参与状态 */
  status?: ParticipationStatus
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

  /** 题目提示，用";"分隔 */
  hints?: string

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
  /** 题目实例的连接方式 */
  instanceEntry?: string | null

  /** 附件 Url */
  url?: string | null

  /** 附件名称 */
  name?: string | null
}

/**
 * 队伍信息更新
 */
export interface TeamUpdateModel {
  /**
   * 队伍名称
   * @pattern [0-9A-Za-z]+
   */
  name?: string | null

  /** 队伍签名 */
  bio?: string | null
}

export type QueryParamsType = Record<string | number, any>
export type ResponseFormat = keyof Omit<Body, 'body' | 'bodyUsed'>

export interface FullRequestParams extends Omit<RequestInit, 'body'> {
  /** set parameter to `true` for call `securityWorker` for this request */
  secure?: boolean
  /** request path */
  path: string
  /** content type of request body */
  type?: ContentType
  /** query params */
  query?: QueryParamsType
  /** format of response (i.e. response.json() -> format: "json") */
  format?: ResponseFormat
  /** request body */
  body?: unknown
  /** base url */
  baseUrl?: string
  /** request cancellation token */
  cancelToken?: CancelToken
}

export type RequestParams = Omit<FullRequestParams, 'body' | 'method' | 'query' | 'path'>

export interface ApiConfig<SecurityDataType = unknown> {
  baseUrl?: string
  baseApiParams?: Omit<RequestParams, 'baseUrl' | 'cancelToken' | 'signal'>
  securityWorker?: (
    securityData: SecurityDataType | null
  ) => Promise<RequestParams | void> | RequestParams | void
  customFetch?: typeof fetch
}

export interface HttpResponse<D extends unknown, E extends unknown = unknown> extends Response {
  data: D
  error: E
}

type CancelToken = Symbol | string | number

export enum ContentType {
  Json = 'application/json',
  FormData = 'multipart/form-data',
  UrlEncoded = 'application/x-www-form-urlencoded',
}

export class HttpClient<SecurityDataType = unknown> {
  public baseUrl: string = ''
  private securityData: SecurityDataType | null = null
  private securityWorker?: ApiConfig<SecurityDataType>['securityWorker']
  private abortControllers = new Map<CancelToken, AbortController>()
  private customFetch = (...fetchParams: Parameters<typeof fetch>) => fetch(...fetchParams)

  private baseApiParams: RequestParams = {
    credentials: 'same-origin',
    headers: {},
    redirect: 'follow',
    referrerPolicy: 'no-referrer',
  }

  constructor(apiConfig: ApiConfig<SecurityDataType> = {}) {
    Object.assign(this, apiConfig)
  }

  public setSecurityData = (data: SecurityDataType | null) => {
    this.securityData = data
  }

  private encodeQueryParam(key: string, value: any) {
    const encodedKey = encodeURIComponent(key)
    return `${encodedKey}=${encodeURIComponent(typeof value === 'number' ? value : `${value}`)}`
  }

  private addQueryParam(query: QueryParamsType, key: string) {
    return this.encodeQueryParam(key, query[key])
  }

  private addArrayQueryParam(query: QueryParamsType, key: string) {
    const value = query[key]
    return value.map((v: any) => this.encodeQueryParam(key, v)).join('&')
  }

  protected toQueryString(rawQuery?: QueryParamsType): string {
    const query = rawQuery || {}
    const keys = Object.keys(query).filter((key) => 'undefined' !== typeof query[key])
    return keys
      .map((key) =>
        Array.isArray(query[key])
          ? this.addArrayQueryParam(query, key)
          : this.addQueryParam(query, key)
      )
      .join('&')
  }

  protected addQueryParams(rawQuery?: QueryParamsType): string {
    const queryString = this.toQueryString(rawQuery)
    return queryString ? `?${queryString}` : ''
  }

  private contentFormatters: Record<ContentType, (input: any) => any> = {
    [ContentType.Json]: (input: any) =>
      input !== null && (typeof input === 'object' || typeof input === 'string')
        ? JSON.stringify(input)
        : input,
    [ContentType.FormData]: (input: any) =>
      Object.keys(input || {}).reduce((formData, key) => {
        const property = input[key]
        formData.append(
          key,
          property instanceof Blob
            ? property
            : typeof property === 'object' && property !== null
            ? JSON.stringify(property)
            : `${property}`
        )
        return formData
      }, new FormData()),
    [ContentType.UrlEncoded]: (input: any) => this.toQueryString(input),
  }

  private mergeRequestParams(params1: RequestParams, params2?: RequestParams): RequestParams {
    return {
      ...this.baseApiParams,
      ...params1,
      ...(params2 || {}),
      headers: {
        ...(this.baseApiParams.headers || {}),
        ...(params1.headers || {}),
        ...((params2 && params2.headers) || {}),
      },
    }
  }

  private createAbortSignal = (cancelToken: CancelToken): AbortSignal | undefined => {
    if (this.abortControllers.has(cancelToken)) {
      const abortController = this.abortControllers.get(cancelToken)
      if (abortController) {
        return abortController.signal
      }
      return void 0
    }

    const abortController = new AbortController()
    this.abortControllers.set(cancelToken, abortController)
    return abortController.signal
  }

  public abortRequest = (cancelToken: CancelToken) => {
    const abortController = this.abortControllers.get(cancelToken)

    if (abortController) {
      abortController.abort()
      this.abortControllers.delete(cancelToken)
    }
  }

  public request = async <T = any, E = any>({
    body,
    secure,
    path,
    type,
    query,
    format,
    baseUrl,
    cancelToken,
    ...params
  }: FullRequestParams): Promise<HttpResponse<T, E>> => {
    const secureParams =
      ((typeof secure === 'boolean' ? secure : this.baseApiParams.secure) &&
        this.securityWorker &&
        (await this.securityWorker(this.securityData))) ||
      {}
    const requestParams = this.mergeRequestParams(params, secureParams)
    const queryString = query && this.toQueryString(query)
    const payloadFormatter = this.contentFormatters[type || ContentType.Json]
    const responseFormat = format || requestParams.format || 'json'

    return this.customFetch(
      `${baseUrl || this.baseUrl || ''}${path}${queryString ? `?${queryString}` : ''}`,
      {
        ...requestParams,
        headers: {
          ...(type && type !== ContentType.FormData ? { 'Content-Type': type } : {}),
          ...(requestParams.headers || {}),
        },
        signal: cancelToken ? this.createAbortSignal(cancelToken) : void 0,
        body: typeof body === 'undefined' || body === null ? null : payloadFormatter(body),
      }
    ).then(async (response) => {
      const r = response as HttpResponse<T, E>
      r.data = null as unknown as T
      r.error = null as unknown as E

      const data = !responseFormat
        ? r
        : await response[responseFormat]()
            .then((data) => {
              if (r.ok) {
                r.data = data
              } else {
                r.error = data
              }
              return r
            })
            .catch((e) => {
              r.error = e
              return r
            })

      if (cancelToken) {
        this.abortControllers.delete(cancelToken)
      }

      if (!response.ok) throw data
      return data
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
      this.request<void, RequestResponse>({
        path: `/api/account/register`,
        method: 'POST',
        body: data,
        type: ContentType.Json,
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
      this.request<void, RequestResponse>({
        path: `/api/account/changeemail`,
        method: 'PUT',
        body: data,
        type: ContentType.Json,
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
    useAccountProfile: (options?: SWRConfiguration) =>
      useSWR<ProfileUserInfoModel, RequestResponse>(`/api/account/profile`, options),

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
    useAdminUsers: (query?: { count?: number; skip?: number }, options?: SWRConfiguration) =>
      useSWR<UserInfoModel[], RequestResponse>([`/api/admin/users`, query], options),

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
    useAdminTeams: (query?: { count?: number; skip?: number }, options?: SWRConfiguration) =>
      useSWR<TeamInfoModel[], RequestResponse>([`/api/admin/teams`, query], options),

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
    useAdminUserInfo: (userid: string, options?: SWRConfiguration) =>
      useSWR<ProfileUserInfoModel, RequestResponse>(`/api/admin/users/${userid}`, options),

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
     * @description 使用此接口删除用户，需要Admin权限
     *
     * @tags Admin
     * @name AdminDeleteUser
     * @summary 删除用户
     * @request DELETE:/api/admin/users/{userid}
     */
    adminDeleteUser: (userid: string, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/admin/users/${userid}`,
        method: 'DELETE',
        ...params,
      }),

    /**
     * @description 使用此接口获取全部日志，需要Admin权限
     *
     * @tags Admin
     * @name AdminLogs
     * @summary 获取全部日志
     * @request GET:/api/admin/logs/{level}
     */
    adminLogs: (
      level: string | null,
      query?: { count?: number; skip?: number },
      params: RequestParams = {}
    ) =>
      this.request<LogMessageModel[], RequestResponse>({
        path: `/api/admin/logs/${level}`,
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
     * @request GET:/api/admin/logs/{level}
     */
    useAdminLogs: (
      level: string | null,
      query?: { count?: number; skip?: number },
      options?: SWRConfiguration
    ) => useSWR<LogMessageModel[], RequestResponse>([`/api/admin/logs/${level}`, query], options),

    /**
     * @description 使用此接口获取全部日志，需要Admin权限
     *
     * @tags Admin
     * @name AdminLogs
     * @summary 获取全部日志
     * @request GET:/api/admin/logs/{level}
     */
    mutateAdminLogs: (
      level: string | null,
      query?: { count?: number; skip?: number },
      data?: LogMessageModel[] | Promise<LogMessageModel[]>,
      options?: MutatorOptions
    ) => mutate<LogMessageModel[]>([`/api/admin/logs/${level}`, query], data, options),

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
    useAdminFiles: (query?: { count?: number; skip?: number }, options?: SWRConfiguration) =>
      useSWR<LocalFile[], RequestResponse>([`/api/admin/files`, query], options),

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
    useAssetsGetFile: (hash: string, filename: string, options?: SWRConfiguration) =>
      useSWR<void, RequestResponse>(`/assets/${hash}/${filename}`, options),

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
    assetsUpload: (data: { files?: File[] }, params: RequestParams = {}) =>
      this.request<LocalFile[], RequestResponse>({
        path: `/api/assets`,
        method: 'POST',
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
     * @description 添加公告，需要管理员权限
     *
     * @tags Edit
     * @name EditAddNotice
     * @summary 添加公告
     * @request POST:/api/edit/notices
     */
    editAddNotice: (data: NoticeModel, params: RequestParams = {}) =>
      this.request<Notice, RequestResponse>({
        path: `/api/edit/notices`,
        method: 'POST',
        body: data,
        type: ContentType.Json,
        format: 'json',
        ...params,
      }),

    /**
     * @description 获取所有公告，需要管理员权限
     *
     * @tags Edit
     * @name EditGetNotices
     * @summary 获取所有公告
     * @request GET:/api/edit/notices
     */
    editGetNotices: (params: RequestParams = {}) =>
      this.request<Notice[], RequestResponse>({
        path: `/api/edit/notices`,
        method: 'GET',
        format: 'json',
        ...params,
      }),
    /**
     * @description 获取所有公告，需要管理员权限
     *
     * @tags Edit
     * @name EditGetNotices
     * @summary 获取所有公告
     * @request GET:/api/edit/notices
     */
    useEditGetNotices: (options?: SWRConfiguration) =>
      useSWR<Notice[], RequestResponse>(`/api/edit/notices`, options),

    /**
     * @description 获取所有公告，需要管理员权限
     *
     * @tags Edit
     * @name EditGetNotices
     * @summary 获取所有公告
     * @request GET:/api/edit/notices
     */
    mutateEditGetNotices: (data?: Notice[] | Promise<Notice[]>, options?: MutatorOptions) =>
      mutate<Notice[]>(`/api/edit/notices`, data, options),

    /**
     * @description 修改公告，需要管理员权限
     *
     * @tags Edit
     * @name EditUpdateNotice
     * @summary 修改公告
     * @request PUT:/api/edit/notices/{id}
     */
    editUpdateNotice: (id: number, data: NoticeModel, params: RequestParams = {}) =>
      this.request<Notice, RequestResponse>({
        path: `/api/edit/notices/${id}`,
        method: 'PUT',
        body: data,
        type: ContentType.Json,
        format: 'json',
        ...params,
      }),

    /**
     * @description 删除公告，需要管理员权限
     *
     * @tags Edit
     * @name EditDeleteNotice
     * @summary 删除公告
     * @request DELETE:/api/edit/notices/{id}
     */
    editDeleteNotice: (id: number, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/edit/notices/${id}`,
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
    useEditGetGames: (query?: { count?: number; skip?: number }, options?: SWRConfiguration) =>
      useSWR<GameInfoModel[], RequestResponse>([`/api/edit/games`, query], options),

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
      this.request<Game, RequestResponse>({
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
    useEditGetGame: (id: number, options?: SWRConfiguration) =>
      useSWR<Game, RequestResponse>(`/api/edit/games/${id}`, options),

    /**
     * @description 获取比赛，需要管理员权限
     *
     * @tags Edit
     * @name EditGetGame
     * @summary 获取比赛
     * @request GET:/api/edit/games/{id}
     */
    mutateEditGetGame: (id: number, data?: Game | Promise<Game>, options?: MutatorOptions) =>
      mutate<Game>(`/api/edit/games/${id}`, data, options),

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
     * @description 添加比赛公告，需要管理员权限
     *
     * @tags Edit
     * @name EditAddGameNotice
     * @summary 添加比赛公告
     * @request POST:/api/edit/games/{id}/notices
     */
    editAddGameNotice: (id: number, data: GameNoticeModel, params: RequestParams = {}) =>
      this.request<GameNotice, RequestResponse>({
        path: `/api/edit/games/${id}/notices`,
        method: 'POST',
        body: data,
        type: ContentType.Json,
        format: 'json',
        ...params,
      }),

    /**
     * @description 获取比赛公告，需要管理员权限
     *
     * @tags Edit
     * @name EditGetGameNotices
     * @summary 获取比赛公告
     * @request GET:/api/edit/games/{id}/notices
     */
    editGetGameNotices: (
      id: number,
      query?: { count?: number; skip?: number },
      params: RequestParams = {}
    ) =>
      this.request<GameNotice, RequestResponse>({
        path: `/api/edit/games/${id}/notices`,
        method: 'GET',
        query: query,
        format: 'json',
        ...params,
      }),
    /**
     * @description 获取比赛公告，需要管理员权限
     *
     * @tags Edit
     * @name EditGetGameNotices
     * @summary 获取比赛公告
     * @request GET:/api/edit/games/{id}/notices
     */
    useEditGetGameNotices: (
      id: number,
      query?: { count?: number; skip?: number },
      options?: SWRConfiguration
    ) => useSWR<GameNotice, RequestResponse>([`/api/edit/games/${id}/notices`, query], options),

    /**
     * @description 获取比赛公告，需要管理员权限
     *
     * @tags Edit
     * @name EditGetGameNotices
     * @summary 获取比赛公告
     * @request GET:/api/edit/games/{id}/notices
     */
    mutateEditGetGameNotices: (
      id: number,
      query?: { count?: number; skip?: number },
      data?: GameNotice | Promise<GameNotice>,
      options?: MutatorOptions
    ) => mutate<GameNotice>([`/api/edit/games/${id}/notices`, query], data, options),

    /**
     * @description 删除比赛公告，需要管理员权限
     *
     * @tags Edit
     * @name EditDeleteGameNotice
     * @summary 删除比赛公告
     * @request DELETE:/api/edit/games/{id}/notices/{noticeId}
     */
    editDeleteGameNotice: (id: number, noticeId: number, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/edit/games/${id}/notices/${noticeId}`,
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
    editAddGameChallenge: (id: number, data: ChallengeModel, params: RequestParams = {}) =>
      this.request<Challenge, RequestResponse>({
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
    editGetGameChallenges: (
      id: number,
      query?: { count?: number; skip?: number },
      params: RequestParams = {}
    ) =>
      this.request<ChallengeInfoModel[], RequestResponse>({
        path: `/api/edit/games/${id}/challenges`,
        method: 'GET',
        query: query,
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
    useEditGetGameChallenges: (
      id: number,
      query?: { count?: number; skip?: number },
      options?: SWRConfiguration
    ) =>
      useSWR<ChallengeInfoModel[], RequestResponse>(
        [`/api/edit/games/${id}/challenges`, query],
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
      query?: { count?: number; skip?: number },
      data?: ChallengeInfoModel[] | Promise<ChallengeInfoModel[]>,
      options?: MutatorOptions
    ) => mutate<ChallengeInfoModel[]>([`/api/edit/games/${id}/challenges`, query], data, options),

    /**
     * @description 获取比赛题目，需要管理员权限
     *
     * @tags Edit
     * @name EditGetGameChallenge
     * @summary 获取比赛题目
     * @request GET:/api/edit/games/{id}/challenges/{cId}
     */
    editGetGameChallenge: (id: number, cId: number, params: RequestParams = {}) =>
      this.request<Challenge, RequestResponse>({
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
    useEditGetGameChallenge: (id: number, cId: number, options?: SWRConfiguration) =>
      useSWR<Challenge, RequestResponse>(`/api/edit/games/${id}/challenges/${cId}`, options),

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
      data?: Challenge | Promise<Challenge>,
      options?: MutatorOptions
    ) => mutate<Challenge>(`/api/edit/games/${id}/challenges/${cId}`, data, options),

    /**
     * @description 修改比赛题目，需要管理员权限
     *
     * @tags Edit
     * @name EditUpdateGameChallenge
     * @summary 修改比赛题目信息
     * @request PUT:/api/edit/games/{id}/challenges/{cId}
     */
    editUpdateGameChallenge: (
      id: number,
      cId: number,
      data: ChallengeModel,
      params: RequestParams = {}
    ) =>
      this.request<Challenge, RequestResponse>({
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
     * @description 添加比赛题目 Flag，需要管理员权限
     *
     * @tags Edit
     * @name EditAddFlag
     * @summary 添加比赛题目 Flag
     * @request POST:/api/edit/games/{id}/challenges/{cId}/flags
     */
    editAddFlag: (id: number, cId: number, data: FlagInfoModel, params: RequestParams = {}) =>
      this.request<number, RequestResponse>({
        path: `/api/edit/games/${id}/challenges/${cId}/flags`,
        method: 'POST',
        body: data,
        type: ContentType.Json,
        format: 'json',
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
    useGameGamesAll: (options?: SWRConfiguration) =>
      useSWR<BasicGameInfoModel[], RequestResponse>(`/api/game`, options),

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
      this.request<GameDetailsModel, RequestResponse>({
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
    useGameGames: (id: number, options?: SWRConfiguration) =>
      useSWR<GameDetailsModel, RequestResponse>(`/api/game/${id}`, options),

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
      data?: GameDetailsModel | Promise<GameDetailsModel>,
      options?: MutatorOptions
    ) => mutate<GameDetailsModel>(`/api/game/${id}`, data, options),

    /**
     * @description 加入一场比赛，需要User权限，需要当前激活队伍的队长权限
     *
     * @tags Game
     * @name GameJoinGame
     * @summary 加入一个比赛
     * @request POST:/api/game/{id}
     */
    gameJoinGame: (id: number, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/game/${id}`,
        method: 'POST',
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
    useGameScoreboard: (id: number, options?: SWRConfiguration) =>
      useSWR<ScoreboardModel, RequestResponse>(`/api/game/${id}/scoreboard`, options),

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
     * @description 获取比赛事件数据
     *
     * @tags Game
     * @name GameNotices
     * @summary 获取比赛事件
     * @request GET:/api/game/{id}/notices
     */
    gameNotices: (id: number, params: RequestParams = {}) =>
      this.request<GameEvent[], RequestResponse>({
        path: `/api/game/${id}/notices`,
        method: 'GET',
        format: 'json',
        ...params,
      }),
    /**
     * @description 获取比赛事件数据
     *
     * @tags Game
     * @name GameNotices
     * @summary 获取比赛事件
     * @request GET:/api/game/{id}/notices
     */
    useGameNotices: (id: number, options?: SWRConfiguration) =>
      useSWR<GameEvent[], RequestResponse>(`/api/game/${id}/notices`, options),

    /**
     * @description 获取比赛事件数据
     *
     * @tags Game
     * @name GameNotices
     * @summary 获取比赛事件
     * @request GET:/api/game/{id}/notices
     */
    mutateGameNotices: (
      id: number,
      data?: GameEvent[] | Promise<GameEvent[]>,
      options?: MutatorOptions
    ) => mutate<GameEvent[]>(`/api/game/${id}/notices`, data, options),

    /**
     * @description 获取比赛提交数据，需要观察者权限
     *
     * @tags Game
     * @name GameSubmissions
     * @summary 获取比赛提交
     * @request GET:/api/game/{id}/submissions
     */
    gameSubmissions: (
      id: number,
      query?: { count?: number; skip?: number },
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
     * @description 获取比赛提交数据，需要观察者权限
     *
     * @tags Game
     * @name GameSubmissions
     * @summary 获取比赛提交
     * @request GET:/api/game/{id}/submissions
     */
    useGameSubmissions: (
      id: number,
      query?: { count?: number; skip?: number },
      options?: SWRConfiguration
    ) => useSWR<Submission[], RequestResponse>([`/api/game/${id}/submissions`, query], options),

    /**
     * @description 获取比赛提交数据，需要观察者权限
     *
     * @tags Game
     * @name GameSubmissions
     * @summary 获取比赛提交
     * @request GET:/api/game/{id}/submissions
     */
    mutateGameSubmissions: (
      id: number,
      query?: { count?: number; skip?: number },
      data?: Submission[] | Promise<Submission[]>,
      options?: MutatorOptions
    ) => mutate<Submission[]>([`/api/game/${id}/submissions`, query], data, options),

    /**
     * @description 获取比赛实例数据，需要观察者权限
     *
     * @tags Game
     * @name GameInstances
     * @summary 获取比赛实例列表
     * @request GET:/api/game/{id}/instances
     */
    gameInstances: (
      id: number,
      query?: { count?: number; skip?: number },
      params: RequestParams = {}
    ) =>
      this.request<InstanceInfoModel[], RequestResponse>({
        path: `/api/game/${id}/instances`,
        method: 'GET',
        query: query,
        format: 'json',
        ...params,
      }),
    /**
     * @description 获取比赛实例数据，需要观察者权限
     *
     * @tags Game
     * @name GameInstances
     * @summary 获取比赛实例列表
     * @request GET:/api/game/{id}/instances
     */
    useGameInstances: (
      id: number,
      query?: { count?: number; skip?: number },
      options?: SWRConfiguration
    ) =>
      useSWR<InstanceInfoModel[], RequestResponse>([`/api/game/${id}/instances`, query], options),

    /**
     * @description 获取比赛实例数据，需要观察者权限
     *
     * @tags Game
     * @name GameInstances
     * @summary 获取比赛实例列表
     * @request GET:/api/game/{id}/instances
     */
    mutateGameInstances: (
      id: number,
      query?: { count?: number; skip?: number },
      data?: InstanceInfoModel[] | Promise<InstanceInfoModel[]>,
      options?: MutatorOptions
    ) => mutate<InstanceInfoModel[]>([`/api/game/${id}/instances`, query], data, options),

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
    useGameChallenges: (id: number, options?: SWRConfiguration) =>
      useSWR<Record<string, ChallengeInfo[]>, RequestResponse>(
        `/api/game/${id}/challenges`,
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
     * @description 获取比赛的全部题目参与信息，需要Admin权限
     *
     * @tags Game
     * @name GameParticipations
     * @summary 获取全部比赛参与信息
     * @request GET:/api/game/{id}/participations
     */
    gameParticipations: (
      id: number,
      query?: { count?: number; skip?: number },
      params: RequestParams = {}
    ) =>
      this.request<ParticipationInfoModel[], RequestResponse>({
        path: `/api/game/${id}/participations`,
        method: 'GET',
        query: query,
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
    useGameParticipations: (
      id: number,
      query?: { count?: number; skip?: number },
      options?: SWRConfiguration
    ) =>
      useSWR<ParticipationInfoModel[], RequestResponse>(
        [`/api/game/${id}/participations`, query],
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
      query?: { count?: number; skip?: number },
      data?: ParticipationInfoModel[] | Promise<ParticipationInfoModel[]>,
      options?: MutatorOptions
    ) => mutate<ParticipationInfoModel[]>([`/api/game/${id}/participations`, query], data, options),

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
    useGameGetChallenge: (id: number, challengeId: number, options?: SWRConfiguration) =>
      useSWR<ChallengeDetailModel, RequestResponse>(
        `/api/game/${id}/challenges/${challengeId}`,
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
     * @description 提交flag，需要User权限，需要当前激活队伍已经报名
     *
     * @tags Game
     * @name GameSubmit
     * @summary 提交 flag
     * @request POST:/api/game/{id}/challenges/{challengeId}
     */
    gameSubmit: (id: number, challengeId: number, data: string, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/game/${id}/challenges/${challengeId}`,
        method: 'POST',
        body: data,
        type: ContentType.Json,
        ...params,
      }),

    /**
     * @description 查询 flag 状态，需要User权限
     *
     * @tags Game
     * @name GameStatus
     * @summary 查询 flag 状态
     * @request GET:/api/game/{id}/status/{submitId}
     */
    gameStatus: (id: number, challengeId: number, submitId: number, params: RequestParams = {}) =>
      this.request<AnswerResult, RequestResponse>({
        path: `/api/game/${id}/status/${submitId}`,
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
     * @request GET:/api/game/{id}/status/{submitId}
     */
    useGameStatus: (
      id: number,
      challengeId: number,
      submitId: number,
      options?: SWRConfiguration
    ) => useSWR<AnswerResult, RequestResponse>(`/api/game/${id}/status/${submitId}`, options),

    /**
     * @description 查询 flag 状态，需要User权限
     *
     * @tags Game
     * @name GameStatus
     * @summary 查询 flag 状态
     * @request GET:/api/game/{id}/status/{submitId}
     */
    mutateGameStatus: (
      id: number,
      challengeId: number,
      submitId: number,
      data?: AnswerResult | Promise<AnswerResult>,
      options?: MutatorOptions
    ) => mutate<AnswerResult>(`/api/game/${id}/status/${submitId}`, data, options),

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
     * @description 获取容器信息，需要User权限
     *
     * @tags Game
     * @name GameGetContainer
     * @summary 获取容器信息
     * @request GET:/api/game/{id}/container/{challengeId}
     */
    gameGetContainer: (id: number, challengeId: number, params: RequestParams = {}) =>
      this.request<ContainerInfoModel, RequestResponse>({
        path: `/api/game/${id}/container/${challengeId}`,
        method: 'GET',
        format: 'json',
        ...params,
      }),
    /**
     * @description 获取容器信息，需要User权限
     *
     * @tags Game
     * @name GameGetContainer
     * @summary 获取容器信息
     * @request GET:/api/game/{id}/container/{challengeId}
     */
    useGameGetContainer: (id: number, challengeId: number, options?: SWRConfiguration) =>
      useSWR<ContainerInfoModel, RequestResponse>(
        `/api/game/${id}/container/${challengeId}`,
        options
      ),

    /**
     * @description 获取容器信息，需要User权限
     *
     * @tags Game
     * @name GameGetContainer
     * @summary 获取容器信息
     * @request GET:/api/game/{id}/container/{challengeId}
     */
    mutateGameGetContainer: (
      id: number,
      challengeId: number,
      data?: ContainerInfoModel | Promise<ContainerInfoModel>,
      options?: MutatorOptions
    ) => mutate<ContainerInfoModel>(`/api/game/${id}/container/${challengeId}`, data, options),

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
     * @description 获取最新公告
     *
     * @tags Info
     * @name InfoGetNotices
     * @summary 获取最新公告
     * @request GET:/api/notices
     */
    infoGetNotices: (params: RequestParams = {}) =>
      this.request<Notice[], any>({
        path: `/api/notices`,
        method: 'GET',
        format: 'json',
        ...params,
      }),
    /**
     * @description 获取最新公告
     *
     * @tags Info
     * @name InfoGetNotices
     * @summary 获取最新公告
     * @request GET:/api/notices
     */
    useInfoGetNotices: (options?: SWRConfiguration) =>
      useSWR<Notice[], any>(`/api/notices`, options),

    /**
     * @description 获取最新公告
     *
     * @tags Info
     * @name InfoGetNotices
     * @summary 获取最新公告
     * @request GET:/api/notices
     */
    mutateInfoGetNotices: (data?: Notice[] | Promise<Notice[]>, options?: MutatorOptions) =>
      mutate<Notice[]>(`/api/notices`, data, options),

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
    useInfoGetRecaptchaSiteKey: (options?: SWRConfiguration) =>
      useSWR<string, any>(`/api/sitekey`, options),

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
    useTeamGetBasicInfo: (id: number, options?: SWRConfiguration) =>
      useSWR<TeamInfoModel, RequestResponse>(`/api/team/${id}`, options),

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
    useTeamGetTeamsInfo: (options?: SWRConfiguration) =>
      useSWR<TeamInfoModel[], RequestResponse>(`/api/team`, options),

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
     * @description 设置队伍为当前激活队伍接口，需要为用户
     *
     * @tags Team
     * @name TeamSetActive
     * @summary 设置队伍为当前激活队伍
     * @request PUT:/api/team/{id}/setactive
     */
    teamSetActive: (id: number, params: RequestParams = {}) =>
      this.request<TeamInfoModel, RequestResponse>({
        path: `/api/team/${id}/setactive`,
        method: 'PUT',
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
    useTeamInviteCode: (id: number, options?: SWRConfiguration) =>
      useSWR<string, RequestResponse>(`/api/team/${id}/invite`, options),

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
     * @request POST:/api/team/accept/{code}
     */
    teamAccept: (code: string, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/team/accept/${code}`,
        method: 'POST',
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
