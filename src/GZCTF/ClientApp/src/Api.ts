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

/** Request response */
export interface RequestResponseOfRegisterStatus {
  /** Response message */
  title?: string;
  /** Data */
  data?: RegisterStatus;
  /**
   * Status code
   * @format int32
   */
  status?: number;
}

/** Login response status */
export enum RegisterStatus {
  LoggedIn = "LoggedIn",
  AdminConfirmationRequired = "AdminConfirmationRequired",
  EmailConfirmationRequired = "EmailConfirmationRequired",
}

/** Request response */
export interface RequestResponse {
  /** Response message */
  title?: string;
  /**
   * Status code
   * @format int32
   */
  status?: number;
}

/** Account registration */
export type RegisterModel = ModelWithCaptcha & {
  /**
   * Username
   * @minLength 3
   * @maxLength 15
   */
  userName: string;
  /**
   * Password
   * @minLength 1
   */
  password: string;
  /**
   * Email
   * @format email
   * @minLength 1
   */
  email: string;
};

export interface ModelWithCaptcha {
  /** Captcha Challenge */
  challenge?: string | null;
}

/** Account recovery */
export type RecoveryModel = ModelWithCaptcha & {
  /**
   * User email
   * @format email
   * @minLength 1
   */
  email: string;
};

/** Account password reset */
export interface PasswordResetModel {
  /**
   * Password
   * @minLength 1
   */
  password: string;
  /**
   * Email
   * @minLength 1
   */
  email: string;
  /**
   * Base64 formatted token received via email
   * @minLength 1
   */
  rToken: string;
}

/** Account verification */
export interface AccountVerifyModel {
  /**
   * Base64 formatted token received via email
   * @minLength 1
   */
  token: string;
  /**
   * Base64 formatted user email
   * @minLength 1
   */
  email: string;
}

/** Login */
export type LoginModel = ModelWithCaptcha & {
  /**
   * Username or email
   * @minLength 1
   */
  userName: string;
  /**
   * Password
   * @minLength 1
   */
  password: string;
};

/** Basic account information update */
export interface ProfileUpdateModel {
  /**
   * Username
   * @minLength 3
   * @maxLength 15
   */
  userName?: string | null;
  /**
   * Description
   * @maxLength 128
   */
  bio?: string | null;
  /**
   * Phone number
   * @format phone
   */
  phone?: string | null;
  /**
   * Real name
   * @maxLength 128
   */
  realName?: string | null;
  /**
   * Student ID
   * @maxLength 64
   */
  stdNumber?: string | null;
}

/** Password change */
export interface PasswordChangeModel {
  /**
   * Old password
   * @minLength 6
   */
  old: string;
  /**
   * New password
   * @minLength 6
   */
  new: string;
}

/** Request response */
export interface RequestResponseOfBoolean {
  /** Response message */
  title?: string;
  /** Data */
  data?: boolean;
  /**
   * Status code
   * @format int32
   */
  status?: number;
}

/** Email change */
export interface MailChangeModel {
  /**
   * New email
   * @format email
   * @minLength 1
   */
  newMail: string;
}

/** Basic account information */
export interface ProfileUserInfoModel {
  /**
   * User ID
   * @format guid
   */
  userId?: string | null;
  /** Username */
  userName?: string | null;
  /** Email */
  email?: string | null;
  /** Bio */
  bio?: string | null;
  /** Phone number */
  phone?: string | null;
  /** Real name */
  realName?: string | null;
  /** Student ID */
  stdNumber?: string | null;
  /** Avatar URL */
  avatar?: string | null;
  /** User role */
  role?: Role | null;
}

/** User role enumeration */
export enum Role {
  Banned = "Banned",
  User = "User",
  Monitor = "Monitor",
  Admin = "Admin",
}

/** Global configuration update */
export interface ConfigEditModel {
  /** User policy */
  accountPolicy?: AccountPolicy | null;
  /** Global configuration */
  globalConfig?: GlobalConfig | null;
  /** Game policy */
  containerPolicy?: ContainerPolicy | null;
}

/** Account policy */
export interface AccountPolicy {
  /** Allow user registration */
  allowRegister?: boolean;
  /** Activate account upon registration */
  activeOnRegister?: boolean;
  /** Use captcha verification */
  useCaptcha?: boolean;
  /** Email confirmation required for registration, email change, and password recovery */
  emailConfirmationRequired?: boolean;
  /** Email domain list, separated by commas */
  emailDomainList?: string;
}

/** Global settings */
export interface GlobalConfig {
  /** Platform prefix name */
  title?: string;
  /** Platform slogan */
  slogan?: string;
  /** Site description information */
  description?: string | null;
  /** Footer information */
  footerInfo?: string | null;
  /** Custom theme color */
  customTheme?: string | null;
  /** Platform logo hash */
  logoHash?: string | null;
  /** Platform favicon hash */
  faviconHash?: string | null;
}

/** Container policy */
export interface ContainerPolicy {
  /** Automatically destroy the oldest container when the limit is reached */
  autoDestroyOnLimitReached?: boolean;
  /**
   * User container limit, used to limit the number of exercise containers
   * @format int32
   */
  maxExerciseContainerCountPerUser?: number;
  /**
   * Default container lifetime in minutes
   * @format int32
   * @min 1
   * @max 7200
   */
  defaultLifetime?: number;
  /**
   * Extension duration for each renewal in minutes
   * @format int32
   * @min 1
   * @max 7200
   */
  extensionDuration?: number;
  /**
   * Renewal window before container stops in minutes
   * @format int32
   * @min 1
   * @max 360
   */
  renewalWindow?: number;
}

/** List response */
export interface ArrayResponseOfUserInfoModel {
  /** Data */
  data: UserInfoModel[];
  /**
   * Data length
   * @format int32
   */
  length: number;
  /**
   * Total length
   * @format int32
   */
  total?: number;
}

/** User information (Admin) */
export interface UserInfoModel {
  /**
   * User ID
   * @format guid
   */
  id?: string | null;
  /** Username */
  userName?: string | null;
  /** Real name */
  realName?: string | null;
  /** Student number */
  stdNumber?: string | null;
  /** Contact phone number */
  phone?: string | null;
  /** Bio */
  bio?: string | null;
  /**
   * Registration time
   * @format uint64
   */
  registerTimeUtc?: number;
  /**
   * Last visit time
   * @format uint64
   */
  lastVisitedUtc?: number;
  /** Last visit IP */
  ip?: string;
  /** Email */
  email?: string | null;
  /** Avatar URL */
  avatar?: string | null;
  /** User role */
  role?: Role | null;
  /** Is email confirmed (can log in) */
  emailConfirmed?: boolean | null;
}

/** Batch user creation (Admin) */
export interface UserCreateModel {
  /**
   * Username
   * @minLength 3
   * @maxLength 15
   */
  userName: string;
  /**
   * Password
   * @minLength 1
   */
  password: string;
  /**
   * Email
   * @format email
   * @minLength 1
   */
  email: string;
  /**
   * Real name
   * @maxLength 128
   */
  realName?: string | null;
  /**
   * Student number
   * @maxLength 64
   */
  stdNumber?: string | null;
  /**
   * Contact phone number
   * @format phone
   */
  phone?: string | null;
  /**
   * Team the user joins
   * @maxLength 15
   */
  teamName?: string | null;
}

/** List response */
export interface ArrayResponseOfTeamInfoModel {
  /** Data */
  data: TeamInfoModel[];
  /**
   * Data length
   * @format int32
   */
  length: number;
  /**
   * Total length
   * @format int32
   */
  total?: number;
}

/** Team information */
export interface TeamInfoModel {
  /**
   * Team ID
   * @format int32
   */
  id?: number;
  /** Team name */
  name?: string | null;
  /** Team bio */
  bio?: string | null;
  /** Avatar URL */
  avatar?: string | null;
  /** Is locked */
  locked?: boolean;
  /** Team members */
  members?: TeamUserInfoModel[] | null;
}

/** Team member information */
export interface TeamUserInfoModel {
  /**
   * User ID
   * @format guid
   */
  id?: string | null;
  /** Username */
  userName?: string | null;
  /** Bio */
  bio?: string | null;
  /** Avatar URL */
  avatar?: string | null;
  /** Is Captain */
  captain?: boolean;
}

/** Team information modification (Admin) */
export interface AdminTeamModel {
  /**
   * Team name
   * @maxLength 20
   */
  name?: string | null;
  /**
   * Team bio
   * @maxLength 72
   */
  bio?: string | null;
  /** Is locked */
  locked?: boolean | null;
}

/** User information modification (Admin) */
export interface AdminUserInfoModel {
  /**
   * Username
   * @minLength 3
   * @maxLength 15
   */
  userName?: string | null;
  /**
   * Email
   * @format email
   */
  email?: string | null;
  /**
   * Signature
   * @maxLength 128
   */
  bio?: string | null;
  /**
   * Phone number
   * @format phone
   */
  phone?: string | null;
  /**
   * Real name
   * @maxLength 128
   */
  realName?: string | null;
  /**
   * Student number
   * @maxLength 64
   */
  stdNumber?: string | null;
  /** Is email confirmed (can log in) */
  emailConfirmed?: boolean | null;
  /** User role */
  role?: Role | null;
}

/** Log information (Admin) */
export interface LogMessageModel {
  /**
   * Log time
   * @format uint64
   */
  time?: number;
  /** Username */
  name?: string | null;
  level?: string | null;
  /** IP address */
  ip?: string | null;
  /** Log message */
  msg?: string | null;
  /** Task status */
  status?: string | null;
}

/** Modify the participation information */
export interface ParticipationEditModel {
  /** Participation Status */
  status?: ParticipationStatus | null;
  /** The division of the participated team */
  division?: string | null;
}

export enum ParticipationStatus {
  Pending = "Pending",
  Accepted = "Accepted",
  Rejected = "Rejected",
  Suspended = "Suspended",
  Unsubmitted = "Unsubmitted",
}

/** Game writeup information */
export interface WriteupInfoModel {
  /**
   * Participation ID
   * @format int32
   */
  id?: number;
  /** Team information */
  team?: TeamInfoModel;
  /** File URL */
  url?: string;
  /**
   * File upload time
   * @format uint64
   */
  uploadTimeUtc?: number;
}

/** List response */
export interface ArrayResponseOfContainerInstanceModel {
  /** Data */
  data: ContainerInstanceModel[];
  /**
   * Data length
   * @format int32
   */
  length: number;
  /**
   * Total length
   * @format int32
   */
  total?: number;
}

/** Container instance information (Admin) */
export interface ContainerInstanceModel {
  /** Team */
  team?: TeamModel | null;
  /** Challenge */
  challenge?: ChallengeModel | null;
  /** Container image */
  image?: string;
  /**
   * Container database ID
   * @format guid
   */
  containerGuid?: string;
  /** Container ID */
  containerId?: string;
  /**
   * Container creation time
   * @format uint64
   */
  startedAt?: number;
  /**
   * Expected container stop time
   * @format uint64
   */
  expectStopAt?: number;
  /** Access IP */
  ip?: string;
  /**
   * Access port
   * @format int32
   */
  port?: number;
}

/** Team information */
export interface TeamModel {
  /**
   * Team ID
   * @format int32
   */
  id?: number;
  /** Team name */
  name?: string;
  /** Team avatar */
  avatar?: string | null;
}

/** Challenge information */
export interface ChallengeModel {
  /**
   * Challenge ID
   * @format int32
   */
  id?: number;
  /** Challenge title */
  title?: string;
  /** Challenge category */
  category?: ChallengeCategory;
}

/** Challenge category */
export enum ChallengeCategory {
  Misc = "Misc",
  Crypto = "Crypto",
  Pwn = "Pwn",
  Web = "Web",
  Reverse = "Reverse",
  Blockchain = "Blockchain",
  Forensics = "Forensics",
  Hardware = "Hardware",
  Mobile = "Mobile",
  PPC = "PPC",
  AI = "AI",
  Pentest = "Pentest",
  OSINT = "OSINT",
}

/** List response */
export interface ArrayResponseOfLocalFile {
  /** Data */
  data: LocalFile[];
  /**
   * Data length
   * @format int32
   */
  length: number;
  /**
   * Total length
   * @format int32
   */
  total?: number;
}

export interface LocalFile {
  /**
   * File hash
   * @maxLength 64
   */
  hash?: string;
  /**
   * File name
   * @minLength 1
   */
  name: string;
}

export interface ProblemDetails {
  type?: string | null;
  title?: string | null;
  /** @format int32 */
  status?: number | null;
  detail?: string | null;
  instance?: string | null;
  [key: string]: any;
}

/** Post item (Edit) */
export interface PostEditModel {
  /**
   * Post title
   * @minLength 1
   * @maxLength 50
   */
  title: string;
  /** Post summary */
  summary?: string;
  /** Post content */
  content?: string;
  /** Post tags */
  tags?: string[] | null;
  /** Is pinned */
  isPinned?: boolean;
}

/** Post details */
export interface PostDetailModel {
  /**
   * Post ID
   * @minLength 1
   */
  id: string;
  /**
   * Post title
   * @minLength 1
   */
  title: string;
  /**
   * Post summary
   * @minLength 1
   */
  summary: string;
  /**
   * Post content
   * @minLength 1
   */
  content: string;
  /** Is pinned */
  isPinned: boolean;
  /** Post tags */
  tags?: string[] | null;
  /** Author avatar */
  authorAvatar?: string | null;
  /** Author name */
  authorName?: string | null;
  /**
   * Publish time
   * @format uint64
   * @minLength 1
   */
  time: number;
}

/** Game information (Edit) */
export interface GameInfoModel {
  /**
   * Game Id
   * @format int32
   */
  id?: number;
  /**
   * Game title
   * @minLength 1
   */
  title: string;
  /** Is hidden */
  hidden?: boolean;
  /** Game summary */
  summary?: string;
  /** Game detailed description */
  content?: string;
  /** Accept teams without review */
  acceptWithoutReview?: boolean;
  /** Is writeup required */
  writeupRequired?: boolean;
  /**
   * Game invitation code
   * @maxLength 32
   */
  inviteCode?: string | null;
  /** List of divisions the game belongs to */
  divisions?: string[] | null;
  /**
   * Team member count limit, 0 means no limit
   * @format int32
   */
  teamMemberCountLimit?: number;
  /**
   * Container count limit per team
   * @format int32
   */
  containerCountLimit?: number;
  /** Game poster URL */
  poster?: string | null;
  /** Game public key */
  publicKey?: string;
  /** Is the game in practice mode (accessible even after the game ends) */
  practiceMode?: boolean;
  /**
   * Start time
   * @format uint64
   * @minLength 1
   */
  start: number;
  /**
   * End time
   * @format uint64
   * @minLength 1
   */
  end: number;
  /**
   * Writeup submission deadline
   * @format uint64
   */
  writeupDeadline?: number;
  /** Writeup additional notes */
  writeupNote?: string;
  /**
   * Blood bonus points
   * @format int64
   */
  bloodBonus?: number;
}

/** List response */
export interface ArrayResponseOfGameInfoModel {
  /** Data */
  data: GameInfoModel[];
  /**
   * Data length
   * @format int32
   */
  length: number;
  /**
   * Total length
   * @format int32
   */
  total?: number;
}

/**
 * Game notice, which will be sent to the client.
 * Information includes first, second, and third blood notifications, hint release notifications, challenging opening notifications, etc.
 */
export type GameNotice = FormattableDataOfNoticeType & {
  /** @format int32 */
  id: number;
  /**
   * Publish time
   * @format uint64
   * @minLength 1
   */
  time: number;
};

/** Formattable data */
export interface FormattableDataOfNoticeType {
  /** Data type */
  type: NoticeType;
  /** List of formatted values */
  values: string[];
}

/** Game announcement type */
export enum NoticeType {
  Normal = "Normal",
  FirstBlood = "FirstBlood",
  SecondBlood = "SecondBlood",
  ThirdBlood = "ThirdBlood",
  NewHint = "NewHint",
  NewChallenge = "NewChallenge",
}

/** Game notice (Edit) */
export interface GameNoticeModel {
  /**
   * Notice content
   * @minLength 1
   */
  content: string;
}

/** Challenge detailed information (Edit) */
export interface ChallengeEditDetailModel {
  /**
   * Challenge Id
   * @format int32
   */
  id?: number;
  /**
   * Challenge title
   * @minLength 1
   */
  title: string;
  /** Challenge content */
  content?: string;
  /** Challenge category */
  category: ChallengeCategory;
  /** Challenge type */
  type: ChallengeType;
  /** Challenge hints */
  hints?: string[];
  /**
   * Flag template, used to generate Flag based on Token and challenge, game information
   * @maxLength 120
   */
  flagTemplate?: string | null;
  /** Is the challenge enabled */
  isEnabled: boolean;
  /**
   * Number of people who passed
   * @format int32
   */
  acceptedCount: number;
  /** Unified file name (only for dynamic attachments) */
  fileName?: string | null;
  /** Challenge attachment (dynamic attachments are stored in FlagInfoModel) */
  attachment?: Attachment | null;
  /** Test container */
  testContainer?: ContainerInfoModel | null;
  /** Challenge Flag information */
  flags: FlagInfoModel[];
  /**
   * Image name and tag
   * @minLength 1
   */
  containerImage: string;
  /**
   * Memory limit (MB)
   * @format int32
   */
  memoryLimit: number;
  /**
   * CPU limit (0.1 CPUs)
   * @format int32
   */
  cpuCount: number;
  /**
   * Storage limit (MB)
   * @format int32
   */
  storageLimit: number;
  /**
   * Container exposed port
   * @format int32
   */
  containerExposePort: number;
  /** Whether to record traffic */
  enableTrafficCapture?: boolean | null;
  /** Whether to disable blood bonus */
  disableBloodBonus?: boolean | null;
  /**
   * Initial score
   * @format int32
   */
  originalScore: number;
  /**
   * Minimum score rate
   * @format double
   * @min 0
   * @max 1
   */
  minScoreRate: number;
  /**
   * Difficulty coefficient
   * @format double
   */
  difficulty: number;
}

export enum ChallengeType {
  StaticAttachment = "StaticAttachment",
  StaticContainer = "StaticContainer",
  DynamicAttachment = "DynamicAttachment",
  DynamicContainer = "DynamicContainer",
}

export interface Attachment {
  /** @format int32 */
  id: number;
  /** Attachment type */
  type: FileType;
  /** Default file URL */
  url?: string | null;
  /**
   * Get attachment size
   * @format int64
   */
  fileSize?: number | null;
}

export enum FileType {
  None = "None",
  Local = "Local",
  Remote = "Remote",
}

export interface ContainerInfoModel {
  /** Container status */
  status?: ContainerStatus;
  /**
   * Container creation time
   * @format uint64
   */
  startedAt?: number;
  /**
   * Expected container stop time
   * @format uint64
   */
  expectStopAt?: number;
  /** Challenge entry point */
  entry?: string;
}

/** Container status */
export enum ContainerStatus {
  Pending = "Pending",
  Running = "Running",
  Destroyed = "Destroyed",
}

/** Flag information (Edit) */
export interface FlagInfoModel {
  /**
   * Flag Id
   * @format int32
   */
  id?: number;
  /** Flag text */
  flag?: string;
  /** Attachment corresponding to the Flag */
  attachment?: Attachment | null;
}

/** Basic challenge information (Edit) */
export interface ChallengeInfoModel {
  /**
   * Challenge Id
   * @format int32
   */
  id?: number;
  /**
   * Challenge title
   * @minLength 1
   */
  title: string;
  /** Challenge category */
  category?: ChallengeCategory;
  /** Challenge type */
  type?: ChallengeType;
  /** Is the challenge enabled */
  isEnabled?: boolean;
  /**
   * Challenge score
   * @format int32
   */
  score?: number;
  /**
   * Minimum score
   * @format int32
   */
  minScore?: number;
  /**
   * Original score
   * @format int32
   */
  originalScore?: number;
}

/** Challenge update information (Edit) */
export interface ChallengeUpdateModel {
  /**
   * Challenge title
   * @minLength 1
   */
  title?: string | null;
  /** Challenge content */
  content?: string | null;
  /**
   * Flag template, used to generate Flag based on Token and challenge/game information
   * @maxLength 120
   */
  flagTemplate?: string | null;
  /** Challenge category */
  category?: ChallengeCategory | null;
  /** Challenge hints */
  hints?: string[] | null;
  /** Is the challenge enabled */
  isEnabled?: boolean | null;
  /** Unified file name */
  fileName?: string | null;
  /** Container image name and tag */
  containerImage?: string | null;
  /**
   * Memory limit (MB)
   * @format int32
   * @min 32
   * @max 1048576
   */
  memoryLimit?: number | null;
  /**
   * CPU limit (0.1 CPUs)
   * @format int32
   * @min 1
   * @max 1024
   */
  cpuCount?: number | null;
  /**
   * Storage limit (MB)
   * @format int32
   * @min 128
   * @max 1048576
   */
  storageLimit?: number | null;
  /**
   * Container exposed port
   * @format int32
   */
  containerExposePort?: number | null;
  /** Is traffic capture enabled */
  enableTrafficCapture?: boolean | null;
  /** Is blood bonus disabled */
  disableBloodBonus?: boolean | null;
  /**
   * Initial score
   * @format int32
   */
  originalScore?: number | null;
  /**
   * Minimum score rate
   * @format double
   * @min 0
   * @max 1
   */
  minScoreRate?: number | null;
  /**
   * Difficulty coefficient
   * @format double
   */
  difficulty?: number | null;
}

/** New attachment information (Edit) */
export interface AttachmentCreateModel {
  /** Attachment type */
  attachmentType?: FileType;
  /** File hash (local file) */
  fileHash?: string | null;
  /** File URL (remote file) */
  remoteUrl?: string | null;
}

/** New Flag information (Edit) */
export interface FlagCreateModel {
  /**
   * Flag text
   * @minLength 1
   * @maxLength 127
   */
  flag: string;
  /** Attachment type */
  attachmentType?: FileType;
  /** File hash (local file) */
  fileHash?: string | null;
  /** File URL (remote file) */
  remoteUrl?: string | null;
}

/** Task execution status */
export enum TaskStatus {
  Success = "Success",
  Failed = "Failed",
  Duplicate = "Duplicate",
  Denied = "Denied",
  NotFound = "NotFound",
  Exit = "Exit",
  Pending = "Pending",
}

/** Basic game information, excluding detailed description and current team registration status */
export interface BasicGameInfoModel {
  /** @format int32 */
  id: number;
  /** Game title */
  title?: string;
  /** Game summary */
  summary?: string;
  /** Poster image URL */
  poster?: string | null;
  /**
   * Team member limit
   * @format int32
   */
  limit?: number;
  /**
   * Start time
   * @format uint64
   * @minLength 1
   */
  start: number;
  /**
   * End time
   * @format uint64
   * @minLength 1
   */
  end: number;
}

/** List response */
export interface ArrayResponseOfBasicGameInfoModel {
  /** Data */
  data: BasicGameInfoModel[];
  /**
   * Data length
   * @format int32
   */
  length: number;
  /**
   * Total length
   * @format int32
   */
  total?: number;
}

/** Detailed game information, including detailed introduction and current team registration status */
export interface DetailedGameInfoModel {
  /** @format int32 */
  id?: number;
  /** Game title */
  title?: string;
  /** Game description */
  summary?: string;
  /** Detailed introduction of the game */
  content?: string;
  /** Whether the game is hidden */
  hidden?: boolean;
  /** List of participation divisions */
  divisions?: string[] | null;
  /** Whether an invitation code is required */
  inviteCodeRequired?: boolean;
  /** Whether writeup submission is required */
  writeupRequired?: boolean;
  /** Game poster URL */
  poster?: string | null;
  /**
   * Team member count limit
   * @format int32
   */
  limit?: number;
  /**
   * Number of teams registered for participation
   * @format int32
   */
  teamCount?: number;
  /** Current registered division */
  division?: string | null;
  /** Team name for participation */
  teamName?: string | null;
  /** Whether the game is in practice mode (can still be accessed after the game ends) */
  practiceMode?: boolean;
  /** Team participation status */
  status?: ParticipationStatus;
  /**
   * Start time
   * @format uint64
   */
  start?: number;
  /**
   * End time
   * @format uint64
   */
  end?: number;
}

export interface GameJoinModel {
  /**
   * Team ID for participation
   * @format int32
   */
  teamId?: number;
  /** Division for participation */
  division?: string | null;
  /** Invitation code for participation */
  inviteCode?: string | null;
}

/** Scoreboard */
export interface ScoreboardModel {
  /**
   * Update time
   * @format uint64
   * @minLength 1
   */
  updateTimeUtc: number;
  /**
   * Blood bonus coefficient
   * @format int64
   */
  bloodBonus: number;
  /** Timeline of the top ten */
  timeLines?: Record<string, TopTimeLine[]>;
  /** List of team information */
  items?: ScoreboardItem[];
  /** Challenge information */
  challenges?: Record<string, ChallengeInfo[]>;
  /**
   * Number of challenges
   * @format int32
   */
  challengeCount?: number;
}

export interface TopTimeLine {
  /**
   * Team ID
   * @format int32
   */
  id?: number;
  /** Team name */
  name?: string;
  /** Timeline */
  items?: TimeLine[];
}

export interface TimeLine {
  /**
   * Time
   * @format uint64
   */
  time?: number;
  /**
   * Score
   * @format int32
   */
  score?: number;
}

export interface ScoreboardItem {
  /**
   * Team ID
   * @format int32
   */
  id?: number;
  /** Team name */
  name?: string;
  /** Team Bio */
  bio?: string | null;
  /** Division of participation */
  division?: string | null;
  /** Team avatar */
  avatar?: string | null;
  /**
   * Score
   * @format int32
   */
  score?: number;
  /**
   * Rank
   * @format int32
   */
  rank?: number;
  /**
   * Division rank
   * @format int32
   */
  divisionRank?: number | null;
  /**
   * Last submission time
   * @format uint64
   */
  lastSubmissionTime?: number;
  /** List of solved challenges */
  solvedChallenges?: ChallengeItem[];
  /**
   * Number of solved challenges
   * @format int32
   */
  solvedCount?: number;
}

export interface ChallengeItem {
  /**
   * Challenge ID
   * @format int32
   */
  id?: number;
  /**
   * Challenge score
   * @format int32
   */
  score?: number;
  /** Submission type (unsolved, first blood, second blood, third blood, or others) */
  type?: SubmissionType;
  /** Username of the solver */
  userName?: string | null;
  /**
   * Submission time for the challenge, used to calculate the timeline
   * @format uint64
   */
  time?: number;
}

/** Submission type */
export enum SubmissionType {
  Unaccepted = "Unaccepted",
  FirstBlood = "FirstBlood",
  SecondBlood = "SecondBlood",
  ThirdBlood = "ThirdBlood",
  Normal = "Normal",
}

export interface ChallengeInfo {
  /**
   * Challenge ID
   * @format int32
   */
  id?: number;
  /** Challenge title */
  title?: string;
  /** Challenge category */
  category?: ChallengeCategory;
  /**
   * Challenge score
   * @format int32
   */
  score?: number;
  /**
   * Number of teams that solved the challenge
   * @format int32
   */
  solved?: number;
  /** Bloods for the challenge */
  bloods?: Blood[];
  /** Whether to disable blood bonus */
  disableBloodBonus?: boolean;
}

export interface Blood {
  /**
   * Team ID
   * @format int32
   */
  id?: number;
  /** Team name */
  name?: string;
  /** Team avatar */
  avatar?: string | null;
  /**
   * Time when the blood was obtained
   * @format uint64
   */
  submitTimeUtc?: number | null;
}

/**
 * Game event, recorded but not sent to the client.
 * Information includes flag submission, container start/stop, cheating, and score changes.
 */
export type GameEvent = FormattableDataOfEventType & {
  /**
   * Publish time
   * @format uint64
   * @minLength 1
   */
  time: number;
  /** Related username */
  user?: string;
  /** Related team name */
  team?: string;
};

/** Formattable data */
export interface FormattableDataOfEventType {
  /** Data type */
  type: EventType;
  /** List of formatted values */
  values: string[];
}

/** Game event type */
export enum EventType {
  Normal = "Normal",
  ContainerStart = "ContainerStart",
  ContainerDestroy = "ContainerDestroy",
  FlagSubmit = "FlagSubmit",
  CheatDetected = "CheatDetected",
}

export interface Submission {
  /**
   * Submitted answer string
   * @maxLength 127
   */
  answer?: string;
  /** Status of the submitted answer */
  status?: AnswerResult;
  /**
   * Time the answer was submitted
   * @format uint64
   */
  time?: number;
  /** User who submitted */
  user?: string;
  /** Team that submitted */
  team?: string;
  /** Challenge that was submitted */
  challenge?: string;
}

/** Judgement result */
export enum AnswerResult {
  FlagSubmitted = "FlagSubmitted",
  Accepted = "Accepted",
  WrongAnswer = "WrongAnswer",
  CheatDetected = "CheatDetected",
  NotFound = "NotFound",
}

/** Cheat behavior information */
export interface CheatInfoModel {
  /** Team owning the flag */
  ownedTeam?: ParticipationModel;
  /** Team submitting the flag */
  submitTeam?: ParticipationModel;
  /** Submission corresponding to this cheating behavior */
  submission?: Submission;
}

/** Team participation information */
export interface ParticipationModel {
  /**
   * Participation ID
   * @format int32
   */
  id?: number;
  /** Team information */
  team?: TeamModel;
  /** Team participation status */
  status?: ParticipationStatus;
  /** Team division */
  division?: string | null;
}

export interface ChallengeTrafficModel {
  /**
   * Challenge ID
   * @format int32
   */
  id?: number;
  /**
   * Challenge title
   * @minLength 1
   */
  title: string;
  /** Challenge category */
  category?: ChallengeCategory;
  /** Challenge type */
  type?: ChallengeType;
  /** Is the challenge enabled */
  isEnabled?: boolean;
  /**
   * Number of team traffic captured by the challenge
   * @format int32
   */
  count?: number;
}

/** Team traffic information */
export interface TeamTrafficModel {
  /**
   * Participation ID
   * @format int32
   */
  id?: number;
  /**
   * Team Id
   * @format int32
   */
  teamId?: number;
  /** Team name */
  name?: string | null;
  /** Division of participation */
  division?: string | null;
  /** Avatar URL */
  avatar?: string | null;
  /**
   * Number of traffic captured by the challenge
   * @format int32
   */
  count?: number;
}

/** File record */
export interface FileRecord {
  /** File name */
  fileName?: string;
  /**
   * File size
   * @format int64
   */
  size?: number;
  /**
   * File modification date
   * @format uint64
   */
  updateTime?: number;
}

export interface GameDetailModel {
  /** Challenge information */
  challenges?: Record<string, ChallengeInfo[]>;
  /**
   * Number of challenges
   * @format int32
   */
  challengeCount?: number;
  /** Scoreboard information */
  rank?: ScoreboardItem | null;
  /**
   * Team token
   * @minLength 1
   */
  teamToken: string;
  /** Whether writeup submission is required */
  writeupRequired: boolean;
  /**
   * Writeup submission deadline
   * @format uint64
   * @minLength 1
   */
  writeupDeadline: number;
}

/** Participation for review (Admin) */
export interface ParticipationInfoModel {
  /**
   * Participation ID
   * @format int32
   */
  id?: number;
  /** Participating team */
  team?: TeamWithDetailedUserInfo;
  /** Registered members */
  registeredMembers?: string[];
  /** Division of the game */
  division?: string | null;
  /** Participation status */
  status?: ParticipationStatus;
}

/** Detailed team information for review (Admin) */
export interface TeamWithDetailedUserInfo {
  /**
   * Team ID
   * @format int32
   */
  id?: number;
  /** Team name */
  name?: string | null;
  /** Team bio */
  bio?: string | null;
  /** Avatar URL */
  avatar?: string | null;
  /** Is locked */
  locked?: boolean;
  /**
   * Captain Id
   * @format guid
   */
  captainId?: string;
  /** Team members */
  members?: ProfileUserInfoModel[] | null;
}

/** Challenge detailed information */
export interface ChallengeDetailModel {
  /**
   * Challenge ID
   * @format int32
   */
  id?: number;
  /** Challenge title */
  title?: string;
  /** Challenge content */
  content?: string;
  /** Challenge category */
  category?: ChallengeCategory;
  /** Challenge hints */
  hints?: string[] | null;
  /**
   * Current score of the challenge
   * @format int32
   */
  score?: number;
  /** Challenge type */
  type?: ChallengeType;
  /** Flag context */
  context?: ClientFlagContext;
}

export interface ClientFlagContext {
  /**
   * Close time of the challenge instance
   * @format uint64
   */
  closeTime?: number | null;
  /** Connection method of the challenge instance */
  instanceEntry?: string | null;
  /** Attachment URL */
  url?: string | null;
  /**
   * Attachment file size
   * @format int64
   */
  fileSize?: number | null;
}

/** Flag submission */
export interface FlagSubmitModel {
  /**
   * Flag content
   * Fix: Prevent accidental submissions from the frontend (number/float/null) that may be incorrectly converted
   * @minLength 1
   * @maxLength 127
   */
  flag: string;
}

/** Game writeup submission information */
export interface BasicWriteupInfoModel {
  /** Whether it has been submitted */
  submitted?: boolean;
  /** File name */
  name?: string;
  /**
   * File size
   * @format int64
   */
  fileSize?: number;
  /** Writeup additional notes */
  note?: string;
}

/** Post information */
export interface PostInfoModel {
  /**
   * Post ID
   * @minLength 1
   */
  id: string;
  /**
   * Post title
   * @minLength 1
   */
  title: string;
  /**
   * Post summary
   * @minLength 1
   */
  summary: string;
  /** Is pinned */
  isPinned: boolean;
  /** Post tags */
  tags?: string[] | null;
  /** Author avatar */
  authorAvatar?: string | null;
  /** Author name */
  authorName?: string | null;
  /**
   * Update time
   * @format uint64
   * @minLength 1
   */
  time: number;
}

/** Client configuration */
export interface ClientConfig {
  /** Platform prefix name */
  title?: string;
  /** Platform slogan */
  slogan?: string;
  /** Footer information */
  footerInfo?: string | null;
  /** Custom theme color */
  customTheme?: string | null;
  /** Platform logo URL */
  logoUrl?: string | null;
  /** Container port mapping type */
  portMapping?: ContainerPortMappingType;
  /**
   * Default container lifetime in minutes
   * @format int32
   */
  defaultLifetime?: number;
  /**
   * Extension duration for each renewal in minutes
   * @format int32
   */
  extensionDuration?: number;
  /**
   * Renewal window before container stops in minutes
   * @format int32
   */
  renewalWindow?: number;
}

export enum ContainerPortMappingType {
  Default = "Default",
  PlatformProxy = "PlatformProxy",
}

/** Client CAPTCHA information */
export interface ClientCaptchaInfoModel {
  /** Captcha Provider Type */
  type?: CaptchaProvider;
  /** Site Key */
  siteKey?: string;
}

export enum CaptchaProvider {
  None = "None",
  HashPow = "HashPow",
  CloudflareTurnstile = "CloudflareTurnstile",
}

/** Hash Pow verification */
export interface HashPowChallenge {
  /** Challenge ID */
  id?: string;
  /** Verification challenge */
  challenge?: string;
  /**
   * Difficulty coefficient
   * @format int32
   */
  difficulty?: number;
}

/** Team information update */
export interface TeamUpdateModel {
  /**
   * Team name
   * @maxLength 20
   */
  name?: string | null;
  /**
   * Team bio
   * @maxLength 72
   */
  bio?: string | null;
}

export interface TeamTransferModel {
  /**
   * New captain ID
   * @format guid
   * @minLength 1
   */
  newCaptainId: string;
}

/** Signature verification */
export interface SignatureVerifyModel {
  /**
   * Team token
   * @minLength 1
   */
  teamToken: string;
  /**
   * Game public key, Base64 encoded
   * @minLength 1
   */
  publicKey: string;
}

import { apiLanguage } from "@Utils/I18n";
import type { AxiosInstance, AxiosRequestConfig, AxiosResponse, HeadersDefaults, ResponseType } from "axios";
import axios from "axios";

export type QueryParamsType = Record<string | number, any>;

export interface FullRequestParams extends Omit<AxiosRequestConfig, "data" | "params" | "url" | "responseType"> {
  /** set parameter to `true` for call `securityWorker` for this request */
  secure?: boolean;
  /** request path */
  path: string;
  /** content type of request body */
  type?: ContentType;
  /** query params */
  query?: QueryParamsType;
  /** format of response (i.e. response.json() -> format: "json") */
  format?: ResponseType;
  /** request body */
  body?: unknown;
}

export type RequestParams = Omit<FullRequestParams, "body" | "method" | "query" | "path">;

export interface ApiConfig<SecurityDataType = unknown> extends Omit<AxiosRequestConfig, "data" | "cancelToken"> {
  securityWorker?: (
    securityData: SecurityDataType | null,
  ) => Promise<AxiosRequestConfig | void> | AxiosRequestConfig | void;
  secure?: boolean;
  format?: ResponseType;
}

export enum ContentType {
  Json = "application/json",
  FormData = "multipart/form-data",
  UrlEncoded = "application/x-www-form-urlencoded",
  Text = "text/plain",
}

export class HttpClient<SecurityDataType = unknown> {
  public instance: AxiosInstance;
  private securityData: SecurityDataType | null = null;
  private securityWorker?: ApiConfig<SecurityDataType>["securityWorker"];
  private secure?: boolean;
  private format?: ResponseType;

  constructor({ securityWorker, secure, format, ...axiosConfig }: ApiConfig<SecurityDataType> = {}) {
    this.instance = axios.create({ ...axiosConfig, baseURL: axiosConfig.baseURL || "" });
    this.secure = secure;
    this.format = format;
    this.securityWorker = securityWorker;
  }

  public setSecurityData = (data: SecurityDataType | null) => {
    this.securityData = data;
  };

  protected mergeRequestParams(params1: AxiosRequestConfig, params2?: AxiosRequestConfig): AxiosRequestConfig {
    const method = params1.method || (params2 && params2.method);

    return {
      ...this.instance.defaults,
      ...params1,
      ...(params2 || {}),
      headers: {
        ...((method && this.instance.defaults.headers[method.toLowerCase() as keyof HeadersDefaults]) || {}),
        ...(params1.headers || {}),
        ...((params2 && params2.headers) || {}),
      },
    };
  }

  protected stringifyFormItem(formItem: unknown) {
    if (typeof formItem === "object" && formItem !== null) {
      return JSON.stringify(formItem);
    } else {
      return `${formItem}`;
    }
  }

  protected createFormData(input: Record<string, unknown>): FormData {
    return Object.keys(input || {}).reduce((formData, key) => {
      const property = input[key];
      const propertyContent: any[] = property instanceof Array ? property : [property];

      for (const formItem of propertyContent) {
        const isFileType = formItem instanceof Blob || formItem instanceof File;
        formData.append(key, isFileType ? formItem : this.stringifyFormItem(formItem));
      }

      return formData;
    }, new FormData());
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
      ((typeof secure === "boolean" ? secure : this.secure) &&
        this.securityWorker &&
        (await this.securityWorker(this.securityData))) ||
      {};
    const requestParams = this.mergeRequestParams(params, secureParams);
    const responseFormat = format || this.format || undefined;

    if (type === ContentType.FormData && body && body !== null && typeof body === "object") {
      body = this.createFormData(body as Record<string, unknown>);
    }

    if (type === ContentType.Text && body && body !== null && typeof body !== "string") {
      body = JSON.stringify(body);
    }

    return this.instance.request({
      ...requestParams,
      headers: {
        ...(requestParams.headers || {}),
        ...(type && type !== ContentType.FormData ? { "Content-Type": type } : {}),
        ...{ "Accept-Language": apiLanguage },
      },
      params: query,
      responseType: responseFormat,
      data: body,
      url: path,
    });
  };
}

import useSWR, { MutatorOptions, SWRConfiguration, mutate } from "swr";

/**
 * @title GZCTF Server API
 * @version v1
 *
 * GZCTF Server API Document
 */
export class Api<SecurityDataType extends unknown> extends HttpClient<SecurityDataType> {
  account = {
    /**
     * @description Use this API to update user's avatar. User permissions required.
     *
     * @tags Account
     * @name AccountAvatar
     * @summary Update user avatar
     * @request PUT:/api/account/avatar
     */
    accountAvatar: (
      data: {
        /** @format binary */
        file?: File | null;
      },
      params: RequestParams = {},
    ) =>
      this.request<string, RequestResponse>({
        path: `/api/account/avatar`,
        method: "PUT",
        body: data,
        type: ContentType.FormData,
        format: "json",
        ...params,
      }),

    /**
     * @description Use this API to change user's email. User permissions required. Email URL: /confirm
     *
     * @tags Account
     * @name AccountChangeEmail
     * @summary User email change
     * @request PUT:/api/account/changeemail
     */
    accountChangeEmail: (data: MailChangeModel, params: RequestParams = {}) =>
      this.request<RequestResponseOfBoolean, RequestResponse>({
        path: `/api/account/changeemail`,
        method: "PUT",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * @description Use this API to change user's password. User permissions required.
     *
     * @tags Account
     * @name AccountChangePassword
     * @summary User password change
     * @request PUT:/api/account/changepassword
     */
    accountChangePassword: (data: PasswordChangeModel, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/account/changepassword`,
        method: "PUT",
        body: data,
        type: ContentType.Json,
        ...params,
      }),

    /**
     * @description Use this API to log in to the account.
     *
     * @tags Account
     * @name AccountLogIn
     * @summary User login
     * @request POST:/api/account/login
     */
    accountLogIn: (data: LoginModel, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/account/login`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        ...params,
      }),

    /**
     * @description Use this API to log out of the account. User permissions required.
     *
     * @tags Account
     * @name AccountLogOut
     * @summary User logout
     * @request POST:/api/account/logout
     */
    accountLogOut: (params: RequestParams = {}) =>
      this.request<void, any>({
        path: `/api/account/logout`,
        method: "POST",
        ...params,
      }),

    /**
     * @description Use this API to confirm email change. Email verification code required. User permissions required.
     *
     * @tags Account
     * @name AccountMailChangeConfirm
     * @summary User email change confirmation
     * @request POST:/api/account/mailchangeconfirm
     */
    accountMailChangeConfirm: (data: AccountVerifyModel, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/account/mailchangeconfirm`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        ...params,
      }),

    /**
     * @description Use this API to reset the password. Email verification code is required.
     *
     * @tags Account
     * @name AccountPasswordReset
     * @summary User password reset
     * @request POST:/api/account/passwordreset
     */
    accountPasswordReset: (data: PasswordResetModel, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/account/passwordreset`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        ...params,
      }),

    /**
     * @description Use this API to get user information. User permissions required.
     *
     * @tags Account
     * @name AccountProfile
     * @summary Get user information
     * @request GET:/api/account/profile
     */
    accountProfile: (params: RequestParams = {}) =>
      this.request<ProfileUserInfoModel, RequestResponse>({
        path: `/api/account/profile`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * @description Use this API to get user information. User permissions required.
     *
     * @tags Account
     * @name AccountProfile
     * @summary Get user information
     * @request GET:/api/account/profile
     */
    useAccountProfile: (options?: SWRConfiguration, doFetch: boolean = true) =>
      useSWR<ProfileUserInfoModel, RequestResponse>(doFetch ? `/api/account/profile` : null, options),

    /**
     * @description Use this API to get user information. User permissions required.
     *
     * @tags Account
     * @name AccountProfile
     * @summary Get user information
     * @request GET:/api/account/profile
     */
    mutateAccountProfile: (data?: ProfileUserInfoModel | Promise<ProfileUserInfoModel>, options?: MutatorOptions) =>
      mutate<ProfileUserInfoModel>(`/api/account/profile`, data, options),

    /**
     * @description Use this API to request password recovery. Sends an email to the user. Email URL: /reset
     *
     * @tags Account
     * @name AccountRecovery
     * @summary User password recovery request
     * @request POST:/api/account/recovery
     */
    accountRecovery: (data: RecoveryModel, params: RequestParams = {}) =>
      this.request<RequestResponse, RequestResponse>({
        path: `/api/account/recovery`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * @description Use this API to register a new user. In development environment, no verification. Email URL: /verify
     *
     * @tags Account
     * @name AccountRegister
     * @summary User registration
     * @request POST:/api/account/register
     */
    accountRegister: (data: RegisterModel, params: RequestParams = {}) =>
      this.request<RequestResponseOfRegisterStatus, RequestResponse>({
        path: `/api/account/register`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * @description Use this API to update username and description. User permissions required.
     *
     * @tags Account
     * @name AccountUpdate
     * @summary User data update
     * @request PUT:/api/account/update
     */
    accountUpdate: (data: ProfileUpdateModel, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/account/update`,
        method: "PUT",
        body: data,
        type: ContentType.Json,
        ...params,
      }),

    /**
     * @description Use this API to confirm email using the verification code.
     *
     * @tags Account
     * @name AccountVerify
     * @summary User email confirmation
     * @request POST:/api/account/verify
     */
    accountVerify: (data: AccountVerifyModel, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/account/verify`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        ...params,
      }),
  };
  admin = {
    /**
     * @description Use this API to add users in batch, requires Admin permission
     *
     * @tags Admin
     * @name AdminAddUsers
     * @summary Add users in batch
     * @request POST:/api/admin/users
     */
    adminAddUsers: (data: UserCreateModel[], params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/admin/users`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        ...params,
      }),

    /**
     * @description Use this API to delete team, requires Admin permission
     *
     * @tags Admin
     * @name AdminDeleteTeam
     * @summary Delete team
     * @request DELETE:/api/admin/teams/{id}
     */
    adminDeleteTeam: (id: number, params: RequestParams = {}) =>
      this.request<string, RequestResponse>({
        path: `/api/admin/teams/${id}`,
        method: "DELETE",
        format: "json",
        ...params,
      }),

    /**
     * @description Use this API to delete user, requires Admin permission
     *
     * @tags Admin
     * @name AdminDeleteUser
     * @summary Delete user
     * @request DELETE:/api/admin/users/{userid}
     */
    adminDeleteUser: (userid: string, params: RequestParams = {}) =>
      this.request<string, RequestResponse>({
        path: `/api/admin/users/${userid}`,
        method: "DELETE",
        format: "json",
        ...params,
      }),

    /**
     * @description Use this API to forcibly delete container instance, requires Admin permission
     *
     * @tags Admin
     * @name AdminDestroyInstance
     * @summary Delete container instance
     * @request DELETE:/api/admin/instances/{id}
     */
    adminDestroyInstance: (id: string, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/admin/instances/${id}`,
        method: "DELETE",
        ...params,
      }),

    /**
     * @description Use this API to download all Writeups, requires Admin permission
     *
     * @tags Admin
     * @name AdminDownloadAllWriteups
     * @summary Download all Writeups
     * @request GET:/api/admin/writeups/{id}/all
     */
    adminDownloadAllWriteups: (id: number, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/admin/writeups/${id}/all`,
        method: "GET",
        ...params,
      }),

    /**
     * @description Use this API to get all files, requires Admin permission
     *
     * @tags Admin
     * @name AdminFiles
     * @summary Get all files
     * @request GET:/api/admin/files
     */
    adminFiles: (
      query?: {
        /**
         * @format int32
         * @min 0
         * @max 500
         * @default 50
         */
        count?: number;
        /**
         * @format int32
         * @default 0
         */
        skip?: number;
      },
      params: RequestParams = {},
    ) =>
      this.request<ArrayResponseOfLocalFile, RequestResponse>({
        path: `/api/admin/files`,
        method: "GET",
        query: query,
        format: "json",
        ...params,
      }),
    /**
     * @description Use this API to get all files, requires Admin permission
     *
     * @tags Admin
     * @name AdminFiles
     * @summary Get all files
     * @request GET:/api/admin/files
     */
    useAdminFiles: (
      query?: {
        /**
         * @format int32
         * @min 0
         * @max 500
         * @default 50
         */
        count?: number;
        /**
         * @format int32
         * @default 0
         */
        skip?: number;
      },
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) => useSWR<ArrayResponseOfLocalFile, RequestResponse>(doFetch ? [`/api/admin/files`, query] : null, options),

    /**
     * @description Use this API to get all files, requires Admin permission
     *
     * @tags Admin
     * @name AdminFiles
     * @summary Get all files
     * @request GET:/api/admin/files
     */
    mutateAdminFiles: (
      query?: {
        /**
         * @format int32
         * @min 0
         * @max 500
         * @default 50
         */
        count?: number;
        /**
         * @format int32
         * @default 0
         */
        skip?: number;
      },
      data?: ArrayResponseOfLocalFile | Promise<ArrayResponseOfLocalFile>,
      options?: MutatorOptions,
    ) => mutate<ArrayResponseOfLocalFile>([`/api/admin/files`, query], data, options),

    /**
     * @description Use this API to get global settings, requires Admin permission
     *
     * @tags Admin
     * @name AdminGetConfigs
     * @summary Get configuration
     * @request GET:/api/admin/config
     */
    adminGetConfigs: (params: RequestParams = {}) =>
      this.request<ConfigEditModel, RequestResponse>({
        path: `/api/admin/config`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * @description Use this API to get global settings, requires Admin permission
     *
     * @tags Admin
     * @name AdminGetConfigs
     * @summary Get configuration
     * @request GET:/api/admin/config
     */
    useAdminGetConfigs: (options?: SWRConfiguration, doFetch: boolean = true) =>
      useSWR<ConfigEditModel, RequestResponse>(doFetch ? `/api/admin/config` : null, options),

    /**
     * @description Use this API to get global settings, requires Admin permission
     *
     * @tags Admin
     * @name AdminGetConfigs
     * @summary Get configuration
     * @request GET:/api/admin/config
     */
    mutateAdminGetConfigs: (data?: ConfigEditModel | Promise<ConfigEditModel>, options?: MutatorOptions) =>
      mutate<ConfigEditModel>(`/api/admin/config`, data, options),

    /**
     * @description Use this API to get all container instances, requires Admin permission
     *
     * @tags Admin
     * @name AdminInstances
     * @summary Get all container instances
     * @request GET:/api/admin/instances
     */
    adminInstances: (params: RequestParams = {}) =>
      this.request<ArrayResponseOfContainerInstanceModel, RequestResponse>({
        path: `/api/admin/instances`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * @description Use this API to get all container instances, requires Admin permission
     *
     * @tags Admin
     * @name AdminInstances
     * @summary Get all container instances
     * @request GET:/api/admin/instances
     */
    useAdminInstances: (options?: SWRConfiguration, doFetch: boolean = true) =>
      useSWR<ArrayResponseOfContainerInstanceModel, RequestResponse>(doFetch ? `/api/admin/instances` : null, options),

    /**
     * @description Use this API to get all container instances, requires Admin permission
     *
     * @tags Admin
     * @name AdminInstances
     * @summary Get all container instances
     * @request GET:/api/admin/instances
     */
    mutateAdminInstances: (
      data?: ArrayResponseOfContainerInstanceModel | Promise<ArrayResponseOfContainerInstanceModel>,
      options?: MutatorOptions,
    ) => mutate<ArrayResponseOfContainerInstanceModel>(`/api/admin/instances`, data, options),

    /**
     * @description Use this API to get all logs, requires Admin permission
     *
     * @tags Admin
     * @name AdminLogs
     * @summary Get all logs
     * @request GET:/api/admin/logs
     */
    adminLogs: (
      query?: {
        /** @default "All" */
        level?: string | null;
        /**
         * @format int32
         * @min 0
         * @max 1000
         * @default 50
         */
        count?: number;
        /**
         * @format int32
         * @default 0
         */
        skip?: number;
      },
      params: RequestParams = {},
    ) =>
      this.request<LogMessageModel[], RequestResponse>({
        path: `/api/admin/logs`,
        method: "GET",
        query: query,
        format: "json",
        ...params,
      }),
    /**
     * @description Use this API to get all logs, requires Admin permission
     *
     * @tags Admin
     * @name AdminLogs
     * @summary Get all logs
     * @request GET:/api/admin/logs
     */
    useAdminLogs: (
      query?: {
        /** @default "All" */
        level?: string | null;
        /**
         * @format int32
         * @min 0
         * @max 1000
         * @default 50
         */
        count?: number;
        /**
         * @format int32
         * @default 0
         */
        skip?: number;
      },
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) => useSWR<LogMessageModel[], RequestResponse>(doFetch ? [`/api/admin/logs`, query] : null, options),

    /**
     * @description Use this API to get all logs, requires Admin permission
     *
     * @tags Admin
     * @name AdminLogs
     * @summary Get all logs
     * @request GET:/api/admin/logs
     */
    mutateAdminLogs: (
      query?: {
        /** @default "All" */
        level?: string | null;
        /**
         * @format int32
         * @min 0
         * @max 1000
         * @default 50
         */
        count?: number;
        /**
         * @format int32
         * @default 0
         */
        skip?: number;
      },
      data?: LogMessageModel[] | Promise<LogMessageModel[]>,
      options?: MutatorOptions,
    ) => mutate<LogMessageModel[]>([`/api/admin/logs`, query], data, options),

    /**
     * @description Use this API to update team participation status, review application, requires Admin permission
     *
     * @tags Admin
     * @name AdminParticipation
     * @summary Update participation status
     * @request PUT:/api/admin/participation/{id}
     */
    adminParticipation: (id: number, data: ParticipationEditModel, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/admin/participation/${id}`,
        method: "PUT",
        body: data,
        type: ContentType.Json,
        ...params,
      }),

    /**
     * @description Use this API to reset the platform Logo, requires Admin permission
     *
     * @tags Admin
     * @name AdminResetLogo
     * @summary Reset platform Logo
     * @request DELETE:/api/admin/config/logo
     */
    adminResetLogo: (params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/admin/config/logo`,
        method: "DELETE",
        ...params,
      }),

    /**
     * @description Use this API to reset user password, requires Admin permission
     *
     * @tags Admin
     * @name AdminResetPassword
     * @summary Reset user password
     * @request DELETE:/api/admin/users/{userid}/password
     */
    adminResetPassword: (userid: string, params: RequestParams = {}) =>
      this.request<string, RequestResponse>({
        path: `/api/admin/users/${userid}/password`,
        method: "DELETE",
        format: "json",
        ...params,
      }),

    /**
     * @description Use this API to search teams, requires Admin permission
     *
     * @tags Admin
     * @name AdminSearchTeams
     * @summary Search teams
     * @request POST:/api/admin/teams/search
     */
    adminSearchTeams: (
      query?: {
        hint?: string;
      },
      params: RequestParams = {},
    ) =>
      this.request<ArrayResponseOfTeamInfoModel, RequestResponse>({
        path: `/api/admin/teams/search`,
        method: "POST",
        query: query,
        format: "json",
        ...params,
      }),

    /**
     * @description Use this API to search users, requires Admin permission
     *
     * @tags Admin
     * @name AdminSearchUsers
     * @summary Search users
     * @request POST:/api/admin/users/search
     */
    adminSearchUsers: (
      query?: {
        hint?: string;
      },
      params: RequestParams = {},
    ) =>
      this.request<ArrayResponseOfUserInfoModel, RequestResponse>({
        path: `/api/admin/users/search`,
        method: "POST",
        query: query,
        format: "json",
        ...params,
      }),

    /**
     * @description Use this API to get all teams, requires Admin permission
     *
     * @tags Admin
     * @name AdminTeams
     * @summary Get all team information
     * @request GET:/api/admin/teams
     */
    adminTeams: (
      query?: {
        /**
         * @format int32
         * @min 0
         * @max 500
         * @default 100
         */
        count?: number;
        /**
         * @format int32
         * @default 0
         */
        skip?: number;
      },
      params: RequestParams = {},
    ) =>
      this.request<ArrayResponseOfTeamInfoModel, RequestResponse>({
        path: `/api/admin/teams`,
        method: "GET",
        query: query,
        format: "json",
        ...params,
      }),
    /**
     * @description Use this API to get all teams, requires Admin permission
     *
     * @tags Admin
     * @name AdminTeams
     * @summary Get all team information
     * @request GET:/api/admin/teams
     */
    useAdminTeams: (
      query?: {
        /**
         * @format int32
         * @min 0
         * @max 500
         * @default 100
         */
        count?: number;
        /**
         * @format int32
         * @default 0
         */
        skip?: number;
      },
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) => useSWR<ArrayResponseOfTeamInfoModel, RequestResponse>(doFetch ? [`/api/admin/teams`, query] : null, options),

    /**
     * @description Use this API to get all teams, requires Admin permission
     *
     * @tags Admin
     * @name AdminTeams
     * @summary Get all team information
     * @request GET:/api/admin/teams
     */
    mutateAdminTeams: (
      query?: {
        /**
         * @format int32
         * @min 0
         * @max 500
         * @default 100
         */
        count?: number;
        /**
         * @format int32
         * @default 0
         */
        skip?: number;
      },
      data?: ArrayResponseOfTeamInfoModel | Promise<ArrayResponseOfTeamInfoModel>,
      options?: MutatorOptions,
    ) => mutate<ArrayResponseOfTeamInfoModel>([`/api/admin/teams`, query], data, options),

    /**
     * @description Use this API to change global settings, requires Admin permission
     *
     * @tags Admin
     * @name AdminUpdateConfigs
     * @summary Change configuration
     * @request PUT:/api/admin/config
     */
    adminUpdateConfigs: (data: ConfigEditModel, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/admin/config`,
        method: "PUT",
        body: data,
        type: ContentType.Json,
        ...params,
      }),

    /**
     * @description Use this API to change the platform Logo, requires Admin permission
     *
     * @tags Admin
     * @name AdminUpdateLogo
     * @summary Change platform Logo
     * @request POST:/api/admin/config/logo
     */
    adminUpdateLogo: (
      data: {
        /** @format binary */
        file?: File | null;
      },
      params: RequestParams = {},
    ) =>
      this.request<void, RequestResponse>({
        path: `/api/admin/config/logo`,
        method: "POST",
        body: data,
        type: ContentType.FormData,
        ...params,
      }),

    /**
     * @description Use this API to modify team information, requires Admin permission
     *
     * @tags Admin
     * @name AdminUpdateTeam
     * @summary Modify team information
     * @request PUT:/api/admin/teams/{id}
     */
    adminUpdateTeam: (id: number, data: AdminTeamModel, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/admin/teams/${id}`,
        method: "PUT",
        body: data,
        type: ContentType.Json,
        ...params,
      }),

    /**
     * @description Use this API to modify user information, requires Admin permission
     *
     * @tags Admin
     * @name AdminUpdateUserInfo
     * @summary Modify user information
     * @request PUT:/api/admin/users/{userid}
     */
    adminUpdateUserInfo: (userid: string, data: AdminUserInfoModel, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/admin/users/${userid}`,
        method: "PUT",
        body: data,
        type: ContentType.Json,
        ...params,
      }),

    /**
     * @description Use this API to get user information, requires Admin permission
     *
     * @tags Admin
     * @name AdminUserInfo
     * @summary Get user information
     * @request GET:/api/admin/users/{userid}
     */
    adminUserInfo: (userid: string, params: RequestParams = {}) =>
      this.request<ProfileUserInfoModel, RequestResponse>({
        path: `/api/admin/users/${userid}`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * @description Use this API to get user information, requires Admin permission
     *
     * @tags Admin
     * @name AdminUserInfo
     * @summary Get user information
     * @request GET:/api/admin/users/{userid}
     */
    useAdminUserInfo: (userid: string, options?: SWRConfiguration, doFetch: boolean = true) =>
      useSWR<ProfileUserInfoModel, RequestResponse>(doFetch ? `/api/admin/users/${userid}` : null, options),

    /**
     * @description Use this API to get user information, requires Admin permission
     *
     * @tags Admin
     * @name AdminUserInfo
     * @summary Get user information
     * @request GET:/api/admin/users/{userid}
     */
    mutateAdminUserInfo: (
      userid: string,
      data?: ProfileUserInfoModel | Promise<ProfileUserInfoModel>,
      options?: MutatorOptions,
    ) => mutate<ProfileUserInfoModel>(`/api/admin/users/${userid}`, data, options),

    /**
     * @description Use this API to get all users, requires Admin permission
     *
     * @tags Admin
     * @name AdminUsers
     * @summary Get all users
     * @request GET:/api/admin/users
     */
    adminUsers: (
      query?: {
        /**
         * @format int32
         * @min 0
         * @max 500
         * @default 100
         */
        count?: number;
        /**
         * @format int32
         * @default 0
         */
        skip?: number;
      },
      params: RequestParams = {},
    ) =>
      this.request<ArrayResponseOfUserInfoModel, RequestResponse>({
        path: `/api/admin/users`,
        method: "GET",
        query: query,
        format: "json",
        ...params,
      }),
    /**
     * @description Use this API to get all users, requires Admin permission
     *
     * @tags Admin
     * @name AdminUsers
     * @summary Get all users
     * @request GET:/api/admin/users
     */
    useAdminUsers: (
      query?: {
        /**
         * @format int32
         * @min 0
         * @max 500
         * @default 100
         */
        count?: number;
        /**
         * @format int32
         * @default 0
         */
        skip?: number;
      },
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) => useSWR<ArrayResponseOfUserInfoModel, RequestResponse>(doFetch ? [`/api/admin/users`, query] : null, options),

    /**
     * @description Use this API to get all users, requires Admin permission
     *
     * @tags Admin
     * @name AdminUsers
     * @summary Get all users
     * @request GET:/api/admin/users
     */
    mutateAdminUsers: (
      query?: {
        /**
         * @format int32
         * @min 0
         * @max 500
         * @default 100
         */
        count?: number;
        /**
         * @format int32
         * @default 0
         */
        skip?: number;
      },
      data?: ArrayResponseOfUserInfoModel | Promise<ArrayResponseOfUserInfoModel>,
      options?: MutatorOptions,
    ) => mutate<ArrayResponseOfUserInfoModel>([`/api/admin/users`, query], data, options),

    /**
     * @description Use this API to get Writeup basic information, requires Admin permission
     *
     * @tags Admin
     * @name AdminWriteups
     * @summary Get all Writeup basic information
     * @request GET:/api/admin/writeups/{id}
     */
    adminWriteups: (id: number, params: RequestParams = {}) =>
      this.request<WriteupInfoModel[], RequestResponse>({
        path: `/api/admin/writeups/${id}`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * @description Use this API to get Writeup basic information, requires Admin permission
     *
     * @tags Admin
     * @name AdminWriteups
     * @summary Get all Writeup basic information
     * @request GET:/api/admin/writeups/{id}
     */
    useAdminWriteups: (id: number, options?: SWRConfiguration, doFetch: boolean = true) =>
      useSWR<WriteupInfoModel[], RequestResponse>(doFetch ? `/api/admin/writeups/${id}` : null, options),

    /**
     * @description Use this API to get Writeup basic information, requires Admin permission
     *
     * @tags Admin
     * @name AdminWriteups
     * @summary Get all Writeup basic information
     * @request GET:/api/admin/writeups/{id}
     */
    mutateAdminWriteups: (
      id: number,
      data?: WriteupInfoModel[] | Promise<WriteupInfoModel[]>,
      options?: MutatorOptions,
    ) => mutate<WriteupInfoModel[]>(`/api/admin/writeups/${id}`, data, options),
  };
  assets = {
    /**
     * @description Delete a file by hash
     *
     * @tags Assets
     * @name AssetsDelete
     * @summary File deletion interface
     * @request DELETE:/api/assets/{hash}
     */
    assetsDelete: (hash: string, params: RequestParams = {}) =>
      this.request<void, RequestResponse | ProblemDetails>({
        path: `/api/assets/${hash}`,
        method: "DELETE",
        ...params,
      }),

    /**
     * @description Retrieve a file by hash, filename is not matched
     *
     * @tags Assets
     * @name AssetsGetFile
     * @summary File retrieval interface
     * @request GET:/assets/{hash}/{filename}
     */
    assetsGetFile: (hash: string, filename: string, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/assets/${hash}/${filename}`,
        method: "GET",
        ...params,
      }),

    /**
     * @description Upload one or more files
     *
     * @tags Assets
     * @name AssetsUpload
     * @summary File upload interface
     * @request POST:/api/assets
     */
    assetsUpload: (
      data: {
        files?: File[] | null;
      },
      query?: {
        /** Unified filename */
        filename?: string | null;
      },
      params: RequestParams = {},
    ) =>
      this.request<LocalFile[], RequestResponse>({
        path: `/api/assets`,
        method: "POST",
        query: query,
        body: data,
        type: ContentType.FormData,
        format: "json",
        ...params,
      }),
  };
  edit = {
    /**
     * @description Adding a game challenge flag requires administrator privileges
     *
     * @tags Edit
     * @name EditAddFlags
     * @summary Add Game Challenge Flag
     * @request POST:/api/edit/games/{id}/challenges/{cId}/flags
     */
    editAddFlags: (id: number, cId: number, data: FlagCreateModel[], params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/edit/games/${id}/challenges/${cId}/flags`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        ...params,
      }),

    /**
     * @description Adding a game requires administrator privileges
     *
     * @tags Edit
     * @name EditAddGame
     * @summary Add Game
     * @request POST:/api/edit/games
     */
    editAddGame: (data: GameInfoModel, params: RequestParams = {}) =>
      this.request<GameInfoModel, RequestResponse>({
        path: `/api/edit/games`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * @description Adding a game challenge requires administrator privileges
     *
     * @tags Edit
     * @name EditAddGameChallenge
     * @summary Add Game Challenge
     * @request POST:/api/edit/games/{id}/challenges
     */
    editAddGameChallenge: (id: number, data: ChallengeInfoModel, params: RequestParams = {}) =>
      this.request<ChallengeEditDetailModel, RequestResponse>({
        path: `/api/edit/games/${id}/challenges`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * @description Adding a game notice requires administrator privileges
     *
     * @tags Edit
     * @name EditAddGameNotice
     * @summary Add Game Notice
     * @request POST:/api/edit/games/{id}/notices
     */
    editAddGameNotice: (id: number, data: GameNoticeModel, params: RequestParams = {}) =>
      this.request<GameNotice, RequestResponse>({
        path: `/api/edit/games/${id}/notices`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * @description Adding a post requires administrator privileges
     *
     * @tags Edit
     * @name EditAddPost
     * @summary Add Post
     * @request POST:/api/edit/posts
     */
    editAddPost: (data: PostEditModel, params: RequestParams = {}) =>
      this.request<string, RequestResponse>({
        path: `/api/edit/posts`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * @description Testing a game challenge container requires administrator privileges
     *
     * @tags Edit
     * @name EditCreateTestContainer
     * @summary Test Game Challenge Container
     * @request POST:/api/edit/games/{id}/challenges/{cId}/container
     */
    editCreateTestContainer: (id: number, cId: number, params: RequestParams = {}) =>
      this.request<ContainerInfoModel, RequestResponse>({
        path: `/api/edit/games/${id}/challenges/${cId}/container`,
        method: "POST",
        format: "json",
        ...params,
      }),

    /**
     * @description Deleting a game requires administrator privileges
     *
     * @tags Edit
     * @name EditDeleteGame
     * @summary Delete Game
     * @request DELETE:/api/edit/games/{id}
     */
    editDeleteGame: (id: number, params: RequestParams = {}) =>
      this.request<GameInfoModel, RequestResponse>({
        path: `/api/edit/games/${id}`,
        method: "DELETE",
        format: "json",
        ...params,
      }),

    /**
     * @description Deleting a game notice requires administrator privileges
     *
     * @tags Edit
     * @name EditDeleteGameNotice
     * @summary Delete Game Notice
     * @request DELETE:/api/edit/games/{id}/notices/{noticeId}
     */
    editDeleteGameNotice: (id: number, noticeId: number, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/edit/games/${id}/notices/${noticeId}`,
        method: "DELETE",
        ...params,
      }),

    /**
     * @description Deleting all WriteUps for a game requires administrator privileges
     *
     * @tags Edit
     * @name EditDeleteGameWriteUps
     * @summary Delete All WriteUps
     * @request DELETE:/api/edit/games/{id}/writeups
     */
    editDeleteGameWriteUps: (id: number, params: RequestParams = {}) =>
      this.request<GameInfoModel, RequestResponse>({
        path: `/api/edit/games/${id}/writeups`,
        method: "DELETE",
        format: "json",
        ...params,
      }),

    /**
     * @description Deleting a post requires administrator privileges
     *
     * @tags Edit
     * @name EditDeletePost
     * @summary Delete Post
     * @request DELETE:/api/edit/posts/{id}
     */
    editDeletePost: (id: string, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/edit/posts/${id}`,
        method: "DELETE",
        ...params,
      }),

    /**
     * @description Destroying a test game challenge container requires administrator privileges
     *
     * @tags Edit
     * @name EditDestroyTestContainer
     * @summary Destroy Test Game Challenge Container
     * @request DELETE:/api/edit/games/{id}/challenges/{cId}/container
     */
    editDestroyTestContainer: (id: number, cId: number, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/edit/games/${id}/challenges/${cId}/container`,
        method: "DELETE",
        ...params,
      }),

    /**
     * @description Retrieving a game requires administrator privileges
     *
     * @tags Edit
     * @name EditGetGame
     * @summary Get Game
     * @request GET:/api/edit/games/{id}
     */
    editGetGame: (id: number, params: RequestParams = {}) =>
      this.request<GameInfoModel, RequestResponse>({
        path: `/api/edit/games/${id}`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * @description Retrieving a game requires administrator privileges
     *
     * @tags Edit
     * @name EditGetGame
     * @summary Get Game
     * @request GET:/api/edit/games/{id}
     */
    useEditGetGame: (id: number, options?: SWRConfiguration, doFetch: boolean = true) =>
      useSWR<GameInfoModel, RequestResponse>(doFetch ? `/api/edit/games/${id}` : null, options),

    /**
     * @description Retrieving a game requires administrator privileges
     *
     * @tags Edit
     * @name EditGetGame
     * @summary Get Game
     * @request GET:/api/edit/games/{id}
     */
    mutateEditGetGame: (id: number, data?: GameInfoModel | Promise<GameInfoModel>, options?: MutatorOptions) =>
      mutate<GameInfoModel>(`/api/edit/games/${id}`, data, options),

    /**
     * @description Retrieving a game challenge requires administrator privileges
     *
     * @tags Edit
     * @name EditGetGameChallenge
     * @summary Get Game Challenge
     * @request GET:/api/edit/games/{id}/challenges/{cId}
     */
    editGetGameChallenge: (id: number, cId: number, params: RequestParams = {}) =>
      this.request<ChallengeEditDetailModel, RequestResponse>({
        path: `/api/edit/games/${id}/challenges/${cId}`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * @description Retrieving a game challenge requires administrator privileges
     *
     * @tags Edit
     * @name EditGetGameChallenge
     * @summary Get Game Challenge
     * @request GET:/api/edit/games/{id}/challenges/{cId}
     */
    useEditGetGameChallenge: (id: number, cId: number, options?: SWRConfiguration, doFetch: boolean = true) =>
      useSWR<ChallengeEditDetailModel, RequestResponse>(
        doFetch ? `/api/edit/games/${id}/challenges/${cId}` : null,
        options,
      ),

    /**
     * @description Retrieving a game challenge requires administrator privileges
     *
     * @tags Edit
     * @name EditGetGameChallenge
     * @summary Get Game Challenge
     * @request GET:/api/edit/games/{id}/challenges/{cId}
     */
    mutateEditGetGameChallenge: (
      id: number,
      cId: number,
      data?: ChallengeEditDetailModel | Promise<ChallengeEditDetailModel>,
      options?: MutatorOptions,
    ) => mutate<ChallengeEditDetailModel>(`/api/edit/games/${id}/challenges/${cId}`, data, options),

    /**
     * @description Retrieving all game challenges requires administrator privileges
     *
     * @tags Edit
     * @name EditGetGameChallenges
     * @summary Get All Game Challenges
     * @request GET:/api/edit/games/{id}/challenges
     */
    editGetGameChallenges: (id: number, params: RequestParams = {}) =>
      this.request<ChallengeInfoModel[], RequestResponse>({
        path: `/api/edit/games/${id}/challenges`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * @description Retrieving all game challenges requires administrator privileges
     *
     * @tags Edit
     * @name EditGetGameChallenges
     * @summary Get All Game Challenges
     * @request GET:/api/edit/games/{id}/challenges
     */
    useEditGetGameChallenges: (id: number, options?: SWRConfiguration, doFetch: boolean = true) =>
      useSWR<ChallengeInfoModel[], RequestResponse>(doFetch ? `/api/edit/games/${id}/challenges` : null, options),

    /**
     * @description Retrieving all game challenges requires administrator privileges
     *
     * @tags Edit
     * @name EditGetGameChallenges
     * @summary Get All Game Challenges
     * @request GET:/api/edit/games/{id}/challenges
     */
    mutateEditGetGameChallenges: (
      id: number,
      data?: ChallengeInfoModel[] | Promise<ChallengeInfoModel[]>,
      options?: MutatorOptions,
    ) => mutate<ChallengeInfoModel[]>(`/api/edit/games/${id}/challenges`, data, options),

    /**
     * @description Retrieving game notices requires administrator privileges
     *
     * @tags Edit
     * @name EditGetGameNotices
     * @summary Get Game Notices
     * @request GET:/api/edit/games/{id}/notices
     */
    editGetGameNotices: (id: number, params: RequestParams = {}) =>
      this.request<GameNotice[], RequestResponse>({
        path: `/api/edit/games/${id}/notices`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * @description Retrieving game notices requires administrator privileges
     *
     * @tags Edit
     * @name EditGetGameNotices
     * @summary Get Game Notices
     * @request GET:/api/edit/games/{id}/notices
     */
    useEditGetGameNotices: (id: number, options?: SWRConfiguration, doFetch: boolean = true) =>
      useSWR<GameNotice[], RequestResponse>(doFetch ? `/api/edit/games/${id}/notices` : null, options),

    /**
     * @description Retrieving game notices requires administrator privileges
     *
     * @tags Edit
     * @name EditGetGameNotices
     * @summary Get Game Notices
     * @request GET:/api/edit/games/{id}/notices
     */
    mutateEditGetGameNotices: (id: number, data?: GameNotice[] | Promise<GameNotice[]>, options?: MutatorOptions) =>
      mutate<GameNotice[]>(`/api/edit/games/${id}/notices`, data, options),

    /**
     * @description Retrieving the game list requires administrator privileges
     *
     * @tags Edit
     * @name EditGetGames
     * @summary Get Game List
     * @request GET:/api/edit/games
     */
    editGetGames: (
      query?: {
        /**
         * @format int32
         * @min 0
         * @max 100
         */
        count?: number;
        /** @format int32 */
        skip?: number;
      },
      params: RequestParams = {},
    ) =>
      this.request<ArrayResponseOfGameInfoModel, RequestResponse>({
        path: `/api/edit/games`,
        method: "GET",
        query: query,
        format: "json",
        ...params,
      }),
    /**
     * @description Retrieving the game list requires administrator privileges
     *
     * @tags Edit
     * @name EditGetGames
     * @summary Get Game List
     * @request GET:/api/edit/games
     */
    useEditGetGames: (
      query?: {
        /**
         * @format int32
         * @min 0
         * @max 100
         */
        count?: number;
        /** @format int32 */
        skip?: number;
      },
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) => useSWR<ArrayResponseOfGameInfoModel, RequestResponse>(doFetch ? [`/api/edit/games`, query] : null, options),

    /**
     * @description Retrieving the game list requires administrator privileges
     *
     * @tags Edit
     * @name EditGetGames
     * @summary Get Game List
     * @request GET:/api/edit/games
     */
    mutateEditGetGames: (
      query?: {
        /**
         * @format int32
         * @min 0
         * @max 100
         */
        count?: number;
        /** @format int32 */
        skip?: number;
      },
      data?: ArrayResponseOfGameInfoModel | Promise<ArrayResponseOfGameInfoModel>,
      options?: MutatorOptions,
    ) => mutate<ArrayResponseOfGameInfoModel>([`/api/edit/games`, query], data, options),

    /**
     * @description Deleting a game challenge flag requires administrator privileges
     *
     * @tags Edit
     * @name EditRemoveFlag
     * @summary Delete Game Challenge Flag
     * @request DELETE:/api/edit/games/{id}/challenges/{cId}/flags/{fId}
     */
    editRemoveFlag: (id: number, cId: number, fId: number, params: RequestParams = {}) =>
      this.request<TaskStatus, RequestResponse>({
        path: `/api/edit/games/${id}/challenges/${cId}/flags/${fId}`,
        method: "DELETE",
        format: "json",
        ...params,
      }),

    /**
     * @description Deleting a game challenge requires administrator privileges
     *
     * @tags Edit
     * @name EditRemoveGameChallenge
     * @summary Delete Game Challenge
     * @request DELETE:/api/edit/games/{id}/challenges/{cId}
     */
    editRemoveGameChallenge: (id: number, cId: number, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/edit/games/${id}/challenges/${cId}`,
        method: "DELETE",
        ...params,
      }),

    /**
     * @description Updating a game challenge attachment requires administrator privileges; only for non-dynamic attachment challenges
     *
     * @tags Edit
     * @name EditUpdateAttachment
     * @summary Update Game Challenge Attachment
     * @request POST:/api/edit/games/{id}/challenges/{cId}/attachment
     */
    editUpdateAttachment: (id: number, cId: number, data: AttachmentCreateModel, params: RequestParams = {}) =>
      this.request<number, RequestResponse>({
        path: `/api/edit/games/${id}/challenges/${cId}/attachment`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * @description Updating a game requires administrator privileges
     *
     * @tags Edit
     * @name EditUpdateGame
     * @summary Update Game
     * @request PUT:/api/edit/games/{id}
     */
    editUpdateGame: (id: number, data: GameInfoModel, params: RequestParams = {}) =>
      this.request<GameInfoModel, RequestResponse>({
        path: `/api/edit/games/${id}`,
        method: "PUT",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * @description Updating a game challenge, requires administrator privileges. Flags are not affected; use Flag-related APIs to modify
     *
     * @tags Edit
     * @name EditUpdateGameChallenge
     * @summary Update Game Challenge Information
     * @request PUT:/api/edit/games/{id}/challenges/{cId}
     */
    editUpdateGameChallenge: (id: number, cId: number, data: ChallengeUpdateModel, params: RequestParams = {}) =>
      this.request<ChallengeEditDetailModel, RequestResponse>({
        path: `/api/edit/games/${id}/challenges/${cId}`,
        method: "PUT",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * @description Updating the accepted count for all game challenges requires administrator privileges
     *
     * @tags Edit
     * @name EditUpdateGameChallengesAcceptedCount
     * @summary Update AC Counter for Challenges
     * @request POST:/api/edit/games/{id}/challenges/updateaccepted
     */
    editUpdateGameChallengesAcceptedCount: (id: number, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/edit/games/${id}/challenges/updateaccepted`,
        method: "POST",
        ...params,
      }),

    /**
     * @description Updating a game notice requires administrator privileges
     *
     * @tags Edit
     * @name EditUpdateGameNotice
     * @summary Update Game Notice
     * @request PUT:/api/edit/games/{id}/notices/{noticeId}
     */
    editUpdateGameNotice: (id: number, noticeId: number, data: GameNoticeModel, params: RequestParams = {}) =>
      this.request<GameNotice, RequestResponse>({
        path: `/api/edit/games/${id}/notices/${noticeId}`,
        method: "PUT",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * @description Use this endpoint to update the game poster; administrator privileges required
     *
     * @tags Edit
     * @name EditUpdateGamePoster
     * @summary Update Game Poster
     * @request PUT:/api/edit/games/{id}/poster
     */
    editUpdateGamePoster: (
      id: number,
      data: {
        /** @format binary */
        file?: File | null;
      },
      params: RequestParams = {},
    ) =>
      this.request<string, RequestResponse>({
        path: `/api/edit/games/${id}/poster`,
        method: "PUT",
        body: data,
        type: ContentType.FormData,
        format: "json",
        ...params,
      }),

    /**
     * @description Updating a post requires administrator privileges
     *
     * @tags Edit
     * @name EditUpdatePost
     * @summary Update Post
     * @request PUT:/api/edit/posts/{id}
     */
    editUpdatePost: (id: string, data: PostEditModel, params: RequestParams = {}) =>
      this.request<PostDetailModel, RequestResponse>({
        path: `/api/edit/posts/${id}`,
        method: "PUT",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),
  };
  game = {
    /**
     * @description Retrieves all challenges of the game; requires User permission and active team participation
     *
     * @tags Game
     * @name GameChallengesWithTeamInfo
     * @summary Get team details in a game
     * @request GET:/api/game/{id}/details
     */
    gameChallengesWithTeamInfo: (id: number, params: RequestParams = {}) =>
      this.request<GameDetailModel, RequestResponse>({
        path: `/api/game/${id}/details`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * @description Retrieves all challenges of the game; requires User permission and active team participation
     *
     * @tags Game
     * @name GameChallengesWithTeamInfo
     * @summary Get team details in a game
     * @request GET:/api/game/{id}/details
     */
    useGameChallengesWithTeamInfo: (id: number, options?: SWRConfiguration, doFetch: boolean = true) =>
      useSWR<GameDetailModel, RequestResponse>(doFetch ? `/api/game/${id}/details` : null, options),

    /**
     * @description Retrieves all challenges of the game; requires User permission and active team participation
     *
     * @tags Game
     * @name GameChallengesWithTeamInfo
     * @summary Get team details in a game
     * @request GET:/api/game/{id}/details
     */
    mutateGameChallengesWithTeamInfo: (
      id: number,
      data?: GameDetailModel | Promise<GameDetailModel>,
      options?: MutatorOptions,
    ) => mutate<GameDetailModel>(`/api/game/${id}/details`, data, options),

    /**
     * @description Retrieves game cheat data; requires Monitor permission
     *
     * @tags Game
     * @name GameCheatInfo
     * @summary Get game cheat information
     * @request GET:/api/game/{id}/cheatinfo
     */
    gameCheatInfo: (id: number, params: RequestParams = {}) =>
      this.request<CheatInfoModel[], RequestResponse>({
        path: `/api/game/${id}/cheatinfo`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * @description Retrieves game cheat data; requires Monitor permission
     *
     * @tags Game
     * @name GameCheatInfo
     * @summary Get game cheat information
     * @request GET:/api/game/{id}/cheatinfo
     */
    useGameCheatInfo: (id: number, options?: SWRConfiguration, doFetch: boolean = true) =>
      useSWR<CheatInfoModel[], RequestResponse>(doFetch ? `/api/game/${id}/cheatinfo` : null, options),

    /**
     * @description Retrieves game cheat data; requires Monitor permission
     *
     * @tags Game
     * @name GameCheatInfo
     * @summary Get game cheat information
     * @request GET:/api/game/{id}/cheatinfo
     */
    mutateGameCheatInfo: (id: number, data?: CheatInfoModel[] | Promise<CheatInfoModel[]>, options?: MutatorOptions) =>
      mutate<CheatInfoModel[]>(`/api/game/${id}/cheatinfo`, data, options),

    /**
     * @description Creates a container; requires User permission
     *
     * @tags Game
     * @name GameCreateContainer
     * @summary Creates a container
     * @request POST:/api/game/{id}/container/{challengeId}
     */
    gameCreateContainer: (id: number, challengeId: number, params: RequestParams = {}) =>
      this.request<ContainerInfoModel, RequestResponse>({
        path: `/api/game/${id}/container/${challengeId}`,
        method: "POST",
        format: "json",
        ...params,
      }),

    /**
     * @description Deletes a team's traffic packet files for a challenge; requires Monitor permission
     *
     * @tags Game
     * @name GameDeleteAllTeamTraffic
     * @summary Deletes all traffic files
     * @request DELETE:/api/game/captures/{challengeId}/{partId}/all
     */
    gameDeleteAllTeamTraffic: (challengeId: number, partId: number, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/game/captures/${challengeId}/${partId}/all`,
        method: "DELETE",
        ...params,
      }),

    /**
     * @description Deletes a container; requires User permission
     *
     * @tags Game
     * @name GameDeleteContainer
     * @summary Deletes a container
     * @request DELETE:/api/game/{id}/container/{challengeId}
     */
    gameDeleteContainer: (id: number, challengeId: number, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/game/${id}/container/${challengeId}`,
        method: "DELETE",
        ...params,
      }),

    /**
     * @description Deletes a traffic packet file; requires Monitor permission
     *
     * @tags Game
     * @name GameDeleteTeamTraffic
     * @summary Deletes a traffic file
     * @request DELETE:/api/game/captures/{challengeId}/{partId}/{filename}
     */
    gameDeleteTeamTraffic: (challengeId: number, partId: number, filename: string, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/game/captures/${challengeId}/${partId}/${filename}`,
        method: "DELETE",
        ...params,
      }),

    /**
     * @description Retrieves game event data; requires Monitor permission
     *
     * @tags Game
     * @name GameEvents
     * @summary Get game events
     * @request GET:/api/game/{id}/events
     */
    gameEvents: (
      id: number,
      query?: {
        /**
         * Hide container events
         * @default false
         */
        hideContainer?: boolean;
        /**
         * @format int32
         * @min 0
         * @max 100
         * @default 100
         */
        count?: number;
        /**
         * @format int32
         * @default 0
         */
        skip?: number;
      },
      params: RequestParams = {},
    ) =>
      this.request<GameEvent[], RequestResponse>({
        path: `/api/game/${id}/events`,
        method: "GET",
        query: query,
        format: "json",
        ...params,
      }),
    /**
     * @description Retrieves game event data; requires Monitor permission
     *
     * @tags Game
     * @name GameEvents
     * @summary Get game events
     * @request GET:/api/game/{id}/events
     */
    useGameEvents: (
      id: number,
      query?: {
        /**
         * Hide container events
         * @default false
         */
        hideContainer?: boolean;
        /**
         * @format int32
         * @min 0
         * @max 100
         * @default 100
         */
        count?: number;
        /**
         * @format int32
         * @default 0
         */
        skip?: number;
      },
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) => useSWR<GameEvent[], RequestResponse>(doFetch ? [`/api/game/${id}/events`, query] : null, options),

    /**
     * @description Retrieves game event data; requires Monitor permission
     *
     * @tags Game
     * @name GameEvents
     * @summary Get game events
     * @request GET:/api/game/{id}/events
     */
    mutateGameEvents: (
      id: number,
      query?: {
        /**
         * Hide container events
         * @default false
         */
        hideContainer?: boolean;
        /**
         * @format int32
         * @min 0
         * @max 100
         * @default 100
         */
        count?: number;
        /**
         * @format int32
         * @default 0
         */
        skip?: number;
      },
      data?: GameEvent[] | Promise<GameEvent[]>,
      options?: MutatorOptions,
    ) => mutate<GameEvent[]>([`/api/game/${id}/events`, query], data, options),

    /**
     * @description Extends container lifetime; requires User permission and can only be extended two hours within ten minutes before expiration
     *
     * @tags Game
     * @name GameExtendContainerLifetime
     * @summary Extends container lifetime
     * @request POST:/api/game/{id}/container/{challengeId}/extend
     */
    gameExtendContainerLifetime: (id: number, challengeId: number, params: RequestParams = {}) =>
      this.request<ContainerInfoModel, RequestResponse>({
        path: `/api/game/${id}/container/${challengeId}/extend`,
        method: "POST",
        format: "json",
        ...params,
      }),

    /**
     * @description Retrieves detailed information about the game
     *
     * @tags Game
     * @name GameGame
     * @summary Get detailed game information
     * @request GET:/api/game/{id}
     */
    gameGame: (id: number, params: RequestParams = {}) =>
      this.request<DetailedGameInfoModel, RequestResponse>({
        path: `/api/game/${id}`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * @description Retrieves detailed information about the game
     *
     * @tags Game
     * @name GameGame
     * @summary Get detailed game information
     * @request GET:/api/game/{id}
     */
    useGameGame: (id: number, options?: SWRConfiguration, doFetch: boolean = true) =>
      useSWR<DetailedGameInfoModel, RequestResponse>(doFetch ? `/api/game/${id}` : null, options),

    /**
     * @description Retrieves detailed information about the game
     *
     * @tags Game
     * @name GameGame
     * @summary Get detailed game information
     * @request GET:/api/game/{id}
     */
    mutateGameGame: (
      id: number,
      data?: DetailedGameInfoModel | Promise<DetailedGameInfoModel>,
      options?: MutatorOptions,
    ) => mutate<DetailedGameInfoModel>(`/api/game/${id}`, data, options),

    /**
     * @description Retrieves game information in specified range
     *
     * @tags Game
     * @name GameGames
     * @summary Get games
     * @request GET:/api/game
     */
    gameGames: (
      query?: {
        /**
         * @format int32
         * @min 0
         * @max 50
         * @default 10
         */
        count?: number;
        /**
         * @format int32
         * @default 0
         */
        skip?: number;
      },
      params: RequestParams = {},
    ) =>
      this.request<ArrayResponseOfBasicGameInfoModel, RequestResponse>({
        path: `/api/game`,
        method: "GET",
        query: query,
        format: "json",
        ...params,
      }),
    /**
     * @description Retrieves game information in specified range
     *
     * @tags Game
     * @name GameGames
     * @summary Get games
     * @request GET:/api/game
     */
    useGameGames: (
      query?: {
        /**
         * @format int32
         * @min 0
         * @max 50
         * @default 10
         */
        count?: number;
        /**
         * @format int32
         * @default 0
         */
        skip?: number;
      },
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) => useSWR<ArrayResponseOfBasicGameInfoModel, RequestResponse>(doFetch ? [`/api/game`, query] : null, options),

    /**
     * @description Retrieves game information in specified range
     *
     * @tags Game
     * @name GameGames
     * @summary Get games
     * @request GET:/api/game
     */
    mutateGameGames: (
      query?: {
        /**
         * @format int32
         * @min 0
         * @max 50
         * @default 10
         */
        count?: number;
        /**
         * @format int32
         * @default 0
         */
        skip?: number;
      },
      data?: ArrayResponseOfBasicGameInfoModel | Promise<ArrayResponseOfBasicGameInfoModel>,
      options?: MutatorOptions,
    ) => mutate<ArrayResponseOfBasicGameInfoModel>([`/api/game`, query], data, options),

    /**
     * @description Downloads all traffic packet files for a team and challenge; requires Monitor permission
     *
     * @tags Game
     * @name GameGetAllTeamTraffic
     * @summary Download all traffic files
     * @request GET:/api/game/captures/{challengeId}/{partId}/all
     */
    gameGetAllTeamTraffic: (challengeId: number, partId: number, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/game/captures/${challengeId}/${partId}/all`,
        method: "GET",
        ...params,
      }),

    /**
     * @description Retrieves challenge information; requires User permission and active team participation
     *
     * @tags Game
     * @name GameGetChallenge
     * @summary Get challenge information
     * @request GET:/api/game/{id}/challenges/{challengeId}
     */
    gameGetChallenge: (id: number, challengeId: number, params: RequestParams = {}) =>
      this.request<ChallengeDetailModel, RequestResponse>({
        path: `/api/game/${id}/challenges/${challengeId}`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * @description Retrieves challenge information; requires User permission and active team participation
     *
     * @tags Game
     * @name GameGetChallenge
     * @summary Get challenge information
     * @request GET:/api/game/{id}/challenges/{challengeId}
     */
    useGameGetChallenge: (id: number, challengeId: number, options?: SWRConfiguration, doFetch: boolean = true) =>
      useSWR<ChallengeDetailModel, RequestResponse>(
        doFetch ? `/api/game/${id}/challenges/${challengeId}` : null,
        options,
      ),

    /**
     * @description Retrieves challenge information; requires User permission and active team participation
     *
     * @tags Game
     * @name GameGetChallenge
     * @summary Get challenge information
     * @request GET:/api/game/{id}/challenges/{challengeId}
     */
    mutateGameGetChallenge: (
      id: number,
      challengeId: number,
      data?: ChallengeDetailModel | Promise<ChallengeDetailModel>,
      options?: MutatorOptions,
    ) => mutate<ChallengeDetailModel>(`/api/game/${id}/challenges/${challengeId}`, data, options),

    /**
     * @description Retrieves challenges with traffic capturing enabled; requires Monitor permission
     *
     * @tags Game
     * @name GameGetChallengesWithTrafficCapturing
     * @summary Get challenges with traffic capturing enabled
     * @request GET:/api/game/games/{id}/captures
     */
    gameGetChallengesWithTrafficCapturing: (id: number, params: RequestParams = {}) =>
      this.request<ChallengeTrafficModel[], RequestResponse>({
        path: `/api/game/games/${id}/captures`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * @description Retrieves challenges with traffic capturing enabled; requires Monitor permission
     *
     * @tags Game
     * @name GameGetChallengesWithTrafficCapturing
     * @summary Get challenges with traffic capturing enabled
     * @request GET:/api/game/games/{id}/captures
     */
    useGameGetChallengesWithTrafficCapturing: (id: number, options?: SWRConfiguration, doFetch: boolean = true) =>
      useSWR<ChallengeTrafficModel[], RequestResponse>(doFetch ? `/api/game/games/${id}/captures` : null, options),

    /**
     * @description Retrieves challenges with traffic capturing enabled; requires Monitor permission
     *
     * @tags Game
     * @name GameGetChallengesWithTrafficCapturing
     * @summary Get challenges with traffic capturing enabled
     * @request GET:/api/game/games/{id}/captures
     */
    mutateGameGetChallengesWithTrafficCapturing: (
      id: number,
      data?: ChallengeTrafficModel[] | Promise<ChallengeTrafficModel[]>,
      options?: MutatorOptions,
    ) => mutate<ChallengeTrafficModel[]>(`/api/game/games/${id}/captures`, data, options),

    /**
     * @description Retrieves the list of captured teams for a game challenge; requires Monitor permission
     *
     * @tags Game
     * @name GameGetChallengeTraffic
     * @summary Get team captures in a challenge
     * @request GET:/api/game/captures/{challengeId}
     */
    gameGetChallengeTraffic: (challengeId: number, params: RequestParams = {}) =>
      this.request<TeamTrafficModel[], RequestResponse>({
        path: `/api/game/captures/${challengeId}`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * @description Retrieves the list of captured teams for a game challenge; requires Monitor permission
     *
     * @tags Game
     * @name GameGetChallengeTraffic
     * @summary Get team captures in a challenge
     * @request GET:/api/game/captures/{challengeId}
     */
    useGameGetChallengeTraffic: (challengeId: number, options?: SWRConfiguration, doFetch: boolean = true) =>
      useSWR<TeamTrafficModel[], RequestResponse>(doFetch ? `/api/game/captures/${challengeId}` : null, options),

    /**
     * @description Retrieves the list of captured teams for a game challenge; requires Monitor permission
     *
     * @tags Game
     * @name GameGetChallengeTraffic
     * @summary Get team captures in a challenge
     * @request GET:/api/game/captures/{challengeId}
     */
    mutateGameGetChallengeTraffic: (
      challengeId: number,
      data?: TeamTrafficModel[] | Promise<TeamTrafficModel[]>,
      options?: MutatorOptions,
    ) => mutate<TeamTrafficModel[]>(`/api/game/captures/${challengeId}`, data, options),

    /**
     * @description Retrieves a traffic packet file; requires Monitor permission
     *
     * @tags Game
     * @name GameGetTeamTraffic
     * @summary Get a traffic file
     * @request GET:/api/game/captures/{challengeId}/{partId}/{filename}
     */
    gameGetTeamTraffic: (challengeId: number, partId: number, filename: string, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/game/captures/${challengeId}/${partId}/${filename}`,
        method: "GET",
        ...params,
      }),

    /**
     * @description Retrieves traffic packet files for a team and challenge; requires Monitor permission
     *
     * @tags Game
     * @name GameGetTeamTrafficAll
     * @summary Get traffic files
     * @request GET:/api/game/captures/{challengeId}/{partId}
     */
    gameGetTeamTrafficAll: (challengeId: number, partId: number, params: RequestParams = {}) =>
      this.request<FileRecord[], RequestResponse>({
        path: `/api/game/captures/${challengeId}/${partId}`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * @description Retrieves traffic packet files for a team and challenge; requires Monitor permission
     *
     * @tags Game
     * @name GameGetTeamTrafficAll
     * @summary Get traffic files
     * @request GET:/api/game/captures/{challengeId}/{partId}
     */
    useGameGetTeamTrafficAll: (
      challengeId: number,
      partId: number,
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) => useSWR<FileRecord[], RequestResponse>(doFetch ? `/api/game/captures/${challengeId}/${partId}` : null, options),

    /**
     * @description Retrieves traffic packet files for a team and challenge; requires Monitor permission
     *
     * @tags Game
     * @name GameGetTeamTrafficAll
     * @summary Get traffic files
     * @request GET:/api/game/captures/{challengeId}/{partId}
     */
    mutateGameGetTeamTrafficAll: (
      challengeId: number,
      partId: number,
      data?: FileRecord[] | Promise<FileRecord[]>,
      options?: MutatorOptions,
    ) => mutate<FileRecord[]>(`/api/game/captures/${challengeId}/${partId}`, data, options),

    /**
     * @description Retrieves post-game writeup submission information; requires User permission
     *
     * @tags Game
     * @name GameGetWriteup
     * @summary Get writeup information
     * @request GET:/api/game/{id}/writeup
     */
    gameGetWriteup: (id: number, params: RequestParams = {}) =>
      this.request<BasicWriteupInfoModel, RequestResponse>({
        path: `/api/game/${id}/writeup`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * @description Retrieves post-game writeup submission information; requires User permission
     *
     * @tags Game
     * @name GameGetWriteup
     * @summary Get writeup information
     * @request GET:/api/game/{id}/writeup
     */
    useGameGetWriteup: (id: number, options?: SWRConfiguration, doFetch: boolean = true) =>
      useSWR<BasicWriteupInfoModel, RequestResponse>(doFetch ? `/api/game/${id}/writeup` : null, options),

    /**
     * @description Retrieves post-game writeup submission information; requires User permission
     *
     * @tags Game
     * @name GameGetWriteup
     * @summary Get writeup information
     * @request GET:/api/game/{id}/writeup
     */
    mutateGameGetWriteup: (
      id: number,
      data?: BasicWriteupInfoModel | Promise<BasicWriteupInfoModel>,
      options?: MutatorOptions,
    ) => mutate<BasicWriteupInfoModel>(`/api/game/${id}/writeup`, data, options),

    /**
     * @description Join a game; requires User permission
     *
     * @tags Game
     * @name GameJoinGame
     * @summary Join a game
     * @request POST:/api/game/{id}
     */
    gameJoinGame: (id: number, data: GameJoinModel, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/game/${id}`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        ...params,
      }),

    /**
     * @description Leave a game; requires User permission
     *
     * @tags Game
     * @name GameLeaveGame
     * @summary Leave a game
     * @request DELETE:/api/game/{id}
     */
    gameLeaveGame: (id: number, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/game/${id}`,
        method: "DELETE",
        ...params,
      }),

    /**
     * @description Retrieves game notice data
     *
     * @tags Game
     * @name GameNotices
     * @summary Get game notices
     * @request GET:/api/game/{id}/notices
     */
    gameNotices: (
      id: number,
      query?: {
        /**
         * @format int32
         * @min 0
         * @max 100
         * @default 100
         */
        count?: number;
        /**
         * @format int32
         * @default 0
         */
        skip?: number;
      },
      params: RequestParams = {},
    ) =>
      this.request<GameNotice[], RequestResponse>({
        path: `/api/game/${id}/notices`,
        method: "GET",
        query: query,
        format: "json",
        ...params,
      }),
    /**
     * @description Retrieves game notice data
     *
     * @tags Game
     * @name GameNotices
     * @summary Get game notices
     * @request GET:/api/game/{id}/notices
     */
    useGameNotices: (
      id: number,
      query?: {
        /**
         * @format int32
         * @min 0
         * @max 100
         * @default 100
         */
        count?: number;
        /**
         * @format int32
         * @default 0
         */
        skip?: number;
      },
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) => useSWR<GameNotice[], RequestResponse>(doFetch ? [`/api/game/${id}/notices`, query] : null, options),

    /**
     * @description Retrieves game notice data
     *
     * @tags Game
     * @name GameNotices
     * @summary Get game notices
     * @request GET:/api/game/{id}/notices
     */
    mutateGameNotices: (
      id: number,
      query?: {
        /**
         * @format int32
         * @min 0
         * @max 100
         * @default 100
         */
        count?: number;
        /**
         * @format int32
         * @default 0
         */
        skip?: number;
      },
      data?: GameNotice[] | Promise<GameNotice[]>,
      options?: MutatorOptions,
    ) => mutate<GameNotice[]>([`/api/game/${id}/notices`, query], data, options),

    /**
     * @description Retrieves all participation information of the game; requires Admin permission
     *
     * @tags Game
     * @name GameParticipations
     * @summary Get all game participations
     * @request GET:/api/game/{id}/participations
     */
    gameParticipations: (id: number, params: RequestParams = {}) =>
      this.request<ParticipationInfoModel[], RequestResponse>({
        path: `/api/game/${id}/participations`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * @description Retrieves all participation information of the game; requires Admin permission
     *
     * @tags Game
     * @name GameParticipations
     * @summary Get all game participations
     * @request GET:/api/game/{id}/participations
     */
    useGameParticipations: (id: number, options?: SWRConfiguration, doFetch: boolean = true) =>
      useSWR<ParticipationInfoModel[], RequestResponse>(doFetch ? `/api/game/${id}/participations` : null, options),

    /**
     * @description Retrieves all participation information of the game; requires Admin permission
     *
     * @tags Game
     * @name GameParticipations
     * @summary Get all game participations
     * @request GET:/api/game/{id}/participations
     */
    mutateGameParticipations: (
      id: number,
      data?: ParticipationInfoModel[] | Promise<ParticipationInfoModel[]>,
      options?: MutatorOptions,
    ) => mutate<ParticipationInfoModel[]>(`/api/game/${id}/participations`, data, options),

    /**
     * @description Retrieves recent game in three weeks
     *
     * @tags Game
     * @name GameRecentGames
     * @summary Get the recent games
     * @request GET:/api/game/recent
     */
    gameRecentGames: (
      query?: {
        /**
         * Limit of the number of games
         * @format int32
         * @min 0
         * @max 50
         */
        limit?: number;
      },
      params: RequestParams = {},
    ) =>
      this.request<BasicGameInfoModel[], RequestResponse>({
        path: `/api/game/recent`,
        method: "GET",
        query: query,
        format: "json",
        ...params,
      }),
    /**
     * @description Retrieves recent game in three weeks
     *
     * @tags Game
     * @name GameRecentGames
     * @summary Get the recent games
     * @request GET:/api/game/recent
     */
    useGameRecentGames: (
      query?: {
        /**
         * Limit of the number of games
         * @format int32
         * @min 0
         * @max 50
         */
        limit?: number;
      },
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) => useSWR<BasicGameInfoModel[], RequestResponse>(doFetch ? [`/api/game/recent`, query] : null, options),

    /**
     * @description Retrieves recent game in three weeks
     *
     * @tags Game
     * @name GameRecentGames
     * @summary Get the recent games
     * @request GET:/api/game/recent
     */
    mutateGameRecentGames: (
      query?: {
        /**
         * Limit of the number of games
         * @format int32
         * @min 0
         * @max 50
         */
        limit?: number;
      },
      data?: BasicGameInfoModel[] | Promise<BasicGameInfoModel[]>,
      options?: MutatorOptions,
    ) => mutate<BasicGameInfoModel[]>([`/api/game/recent`, query], data, options),

    /**
     * @description Retrieves the scoreboard data
     *
     * @tags Game
     * @name GameScoreboard
     * @summary Get the scoreboard
     * @request GET:/api/game/{id}/scoreboard
     */
    gameScoreboard: (id: number, params: RequestParams = {}) =>
      this.request<ScoreboardModel, RequestResponse>({
        path: `/api/game/${id}/scoreboard`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * @description Retrieves the scoreboard data
     *
     * @tags Game
     * @name GameScoreboard
     * @summary Get the scoreboard
     * @request GET:/api/game/{id}/scoreboard
     */
    useGameScoreboard: (id: number, options?: SWRConfiguration, doFetch: boolean = true) =>
      useSWR<ScoreboardModel, RequestResponse>(doFetch ? `/api/game/${id}/scoreboard` : null, options),

    /**
     * @description Retrieves the scoreboard data
     *
     * @tags Game
     * @name GameScoreboard
     * @summary Get the scoreboard
     * @request GET:/api/game/{id}/scoreboard
     */
    mutateGameScoreboard: (id: number, data?: ScoreboardModel | Promise<ScoreboardModel>, options?: MutatorOptions) =>
      mutate<ScoreboardModel>(`/api/game/${id}/scoreboard`, data, options),

    /**
     * @description Downloads the game scoreboard; requires Monitor permission
     *
     * @tags Game
     * @name GameScoreboardSheet
     * @summary Downloads the scoreboard
     * @request GET:/api/game/{id}/scoreboardsheet
     */
    gameScoreboardSheet: (id: number, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/game/${id}/scoreboardsheet`,
        method: "GET",
        ...params,
      }),

    /**
     * @description Queries flag status; requires User permission
     *
     * @tags Game
     * @name GameStatus
     * @summary Queries flag status
     * @request GET:/api/game/{id}/challenges/{challengeId}/status/{submitId}
     */
    gameStatus: (id: number, challengeId: number, submitId: number, params: RequestParams = {}) =>
      this.request<AnswerResult, RequestResponse>({
        path: `/api/game/${id}/challenges/${challengeId}/status/${submitId}`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * @description Queries flag status; requires User permission
     *
     * @tags Game
     * @name GameStatus
     * @summary Queries flag status
     * @request GET:/api/game/{id}/challenges/{challengeId}/status/{submitId}
     */
    useGameStatus: (
      id: number,
      challengeId: number,
      submitId: number,
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<AnswerResult, RequestResponse>(
        doFetch ? `/api/game/${id}/challenges/${challengeId}/status/${submitId}` : null,
        options,
      ),

    /**
     * @description Queries flag status; requires User permission
     *
     * @tags Game
     * @name GameStatus
     * @summary Queries flag status
     * @request GET:/api/game/{id}/challenges/{challengeId}/status/{submitId}
     */
    mutateGameStatus: (
      id: number,
      challengeId: number,
      submitId: number,
      data?: AnswerResult | Promise<AnswerResult>,
      options?: MutatorOptions,
    ) => mutate<AnswerResult>(`/api/game/${id}/challenges/${challengeId}/status/${submitId}`, data, options),

    /**
     * @description Retrieves game submission data; requires Monitor permission
     *
     * @tags Game
     * @name GameSubmissions
     * @summary Get game submissions
     * @request GET:/api/game/{id}/submissions
     */
    gameSubmissions: (
      id: number,
      query?: {
        /** Submission type */
        type?: AnswerResult | null;
        /**
         * @format int32
         * @min 0
         * @max 100
         * @default 100
         */
        count?: number;
        /**
         * @format int32
         * @default 0
         */
        skip?: number;
      },
      params: RequestParams = {},
    ) =>
      this.request<Submission[], RequestResponse>({
        path: `/api/game/${id}/submissions`,
        method: "GET",
        query: query,
        format: "json",
        ...params,
      }),
    /**
     * @description Retrieves game submission data; requires Monitor permission
     *
     * @tags Game
     * @name GameSubmissions
     * @summary Get game submissions
     * @request GET:/api/game/{id}/submissions
     */
    useGameSubmissions: (
      id: number,
      query?: {
        /** Submission type */
        type?: AnswerResult | null;
        /**
         * @format int32
         * @min 0
         * @max 100
         * @default 100
         */
        count?: number;
        /**
         * @format int32
         * @default 0
         */
        skip?: number;
      },
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) => useSWR<Submission[], RequestResponse>(doFetch ? [`/api/game/${id}/submissions`, query] : null, options),

    /**
     * @description Retrieves game submission data; requires Monitor permission
     *
     * @tags Game
     * @name GameSubmissions
     * @summary Get game submissions
     * @request GET:/api/game/{id}/submissions
     */
    mutateGameSubmissions: (
      id: number,
      query?: {
        /** Submission type */
        type?: AnswerResult | null;
        /**
         * @format int32
         * @min 0
         * @max 100
         * @default 100
         */
        count?: number;
        /**
         * @format int32
         * @default 0
         */
        skip?: number;
      },
      data?: Submission[] | Promise<Submission[]>,
      options?: MutatorOptions,
    ) => mutate<Submission[]>([`/api/game/${id}/submissions`, query], data, options),

    /**
     * @description Downloads all submissions of the game; requires Monitor permission
     *
     * @tags Game
     * @name GameSubmissionSheet
     * @summary Downloads all submissions
     * @request GET:/api/game/{id}/submissionsheet
     */
    gameSubmissionSheet: (id: number, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/game/${id}/submissionsheet`,
        method: "GET",
        ...params,
      }),

    /**
     * @description Submits a flag; requires User permission and active team participation
     *
     * @tags Game
     * @name GameSubmit
     * @summary Submits a flag
     * @request POST:/api/game/{id}/challenges/{challengeId}
     */
    gameSubmit: (id: number, challengeId: number, data: FlagSubmitModel, params: RequestParams = {}) =>
      this.request<number, RequestResponse>({
        path: `/api/game/${id}/challenges/${challengeId}`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * @description Submits a post-game writeup; requires User permission
     *
     * @tags Game
     * @name GameSubmitWriteup
     * @summary Submits a writeup
     * @request POST:/api/game/{id}/writeup
     */
    gameSubmitWriteup: (
      id: number,
      data: {
        /** @format binary */
        file?: File | null;
      },
      params: RequestParams = {},
    ) =>
      this.request<void, RequestResponse>({
        path: `/api/game/${id}/writeup`,
        method: "POST",
        body: data,
        type: ContentType.FormData,
        ...params,
      }),
  };
  info = {
    /**
     * @description Get Captcha configuration
     *
     * @tags Info
     * @name InfoGetClientCaptchaInfo
     * @summary Get Captcha configuration
     * @request GET:/api/captcha
     */
    infoGetClientCaptchaInfo: (params: RequestParams = {}) =>
      this.request<ClientCaptchaInfoModel, any>({
        path: `/api/captcha`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * @description Get Captcha configuration
     *
     * @tags Info
     * @name InfoGetClientCaptchaInfo
     * @summary Get Captcha configuration
     * @request GET:/api/captcha
     */
    useInfoGetClientCaptchaInfo: (options?: SWRConfiguration, doFetch: boolean = true) =>
      useSWR<ClientCaptchaInfoModel, any>(doFetch ? `/api/captcha` : null, options),

    /**
     * @description Get Captcha configuration
     *
     * @tags Info
     * @name InfoGetClientCaptchaInfo
     * @summary Get Captcha configuration
     * @request GET:/api/captcha
     */
    mutateInfoGetClientCaptchaInfo: (
      data?: ClientCaptchaInfoModel | Promise<ClientCaptchaInfoModel>,
      options?: MutatorOptions,
    ) => mutate<ClientCaptchaInfoModel>(`/api/captcha`, data, options),

    /**
     * @description Get client configuration
     *
     * @tags Info
     * @name InfoGetClientConfig
     * @summary Get client configuration
     * @request GET:/api/config
     */
    infoGetClientConfig: (params: RequestParams = {}) =>
      this.request<ClientConfig, any>({
        path: `/api/config`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * @description Get client configuration
     *
     * @tags Info
     * @name InfoGetClientConfig
     * @summary Get client configuration
     * @request GET:/api/config
     */
    useInfoGetClientConfig: (options?: SWRConfiguration, doFetch: boolean = true) =>
      useSWR<ClientConfig, any>(doFetch ? `/api/config` : null, options),

    /**
     * @description Get client configuration
     *
     * @tags Info
     * @name InfoGetClientConfig
     * @summary Get client configuration
     * @request GET:/api/config
     */
    mutateInfoGetClientConfig: (data?: ClientConfig | Promise<ClientConfig>, options?: MutatorOptions) =>
      mutate<ClientConfig>(`/api/config`, data, options),

    /**
     * @description Get the latest posts
     *
     * @tags Info
     * @name InfoGetLatestPosts
     * @summary Get the latest posts
     * @request GET:/api/posts/latest
     */
    infoGetLatestPosts: (params: RequestParams = {}) =>
      this.request<PostInfoModel[], any>({
        path: `/api/posts/latest`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * @description Get the latest posts
     *
     * @tags Info
     * @name InfoGetLatestPosts
     * @summary Get the latest posts
     * @request GET:/api/posts/latest
     */
    useInfoGetLatestPosts: (options?: SWRConfiguration, doFetch: boolean = true) =>
      useSWR<PostInfoModel[], any>(doFetch ? `/api/posts/latest` : null, options),

    /**
     * @description Get the latest posts
     *
     * @tags Info
     * @name InfoGetLatestPosts
     * @summary Get the latest posts
     * @request GET:/api/posts/latest
     */
    mutateInfoGetLatestPosts: (data?: PostInfoModel[] | Promise<PostInfoModel[]>, options?: MutatorOptions) =>
      mutate<PostInfoModel[]>(`/api/posts/latest`, data, options),

    /**
     * @description Get post details
     *
     * @tags Info
     * @name InfoGetPost
     * @summary Get post details
     * @request GET:/api/posts/{id}
     */
    infoGetPost: (id: string, params: RequestParams = {}) =>
      this.request<PostDetailModel, RequestResponse>({
        path: `/api/posts/${id}`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * @description Get post details
     *
     * @tags Info
     * @name InfoGetPost
     * @summary Get post details
     * @request GET:/api/posts/{id}
     */
    useInfoGetPost: (id: string, options?: SWRConfiguration, doFetch: boolean = true) =>
      useSWR<PostDetailModel, RequestResponse>(doFetch ? `/api/posts/${id}` : null, options),

    /**
     * @description Get post details
     *
     * @tags Info
     * @name InfoGetPost
     * @summary Get post details
     * @request GET:/api/posts/{id}
     */
    mutateInfoGetPost: (id: string, data?: PostDetailModel | Promise<PostDetailModel>, options?: MutatorOptions) =>
      mutate<PostDetailModel>(`/api/posts/${id}`, data, options),

    /**
     * @description Get all posts
     *
     * @tags Info
     * @name InfoGetPosts
     * @summary Get all posts
     * @request GET:/api/posts
     */
    infoGetPosts: (params: RequestParams = {}) =>
      this.request<PostInfoModel[], any>({
        path: `/api/posts`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * @description Get all posts
     *
     * @tags Info
     * @name InfoGetPosts
     * @summary Get all posts
     * @request GET:/api/posts
     */
    useInfoGetPosts: (options?: SWRConfiguration, doFetch: boolean = true) =>
      useSWR<PostInfoModel[], any>(doFetch ? `/api/posts` : null, options),

    /**
     * @description Get all posts
     *
     * @tags Info
     * @name InfoGetPosts
     * @summary Get all posts
     * @request GET:/api/posts
     */
    mutateInfoGetPosts: (data?: PostInfoModel[] | Promise<PostInfoModel[]>, options?: MutatorOptions) =>
      mutate<PostInfoModel[]>(`/api/posts`, data, options),

    /**
     * @description Create Pow Captcha, valid for 5 minutes
     *
     * @tags Info
     * @name InfoPowChallenge
     * @summary Create Pow Captcha
     * @request GET:/api/captcha/powchallenge
     */
    infoPowChallenge: (params: RequestParams = {}) =>
      this.request<HashPowChallenge, RequestResponse>({
        path: `/api/captcha/powchallenge`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * @description Create Pow Captcha, valid for 5 minutes
     *
     * @tags Info
     * @name InfoPowChallenge
     * @summary Create Pow Captcha
     * @request GET:/api/captcha/powchallenge
     */
    useInfoPowChallenge: (options?: SWRConfiguration, doFetch: boolean = true) =>
      useSWR<HashPowChallenge, RequestResponse>(doFetch ? `/api/captcha/powchallenge` : null, options),

    /**
     * @description Create Pow Captcha, valid for 5 minutes
     *
     * @tags Info
     * @name InfoPowChallenge
     * @summary Create Pow Captcha
     * @request GET:/api/captcha/powchallenge
     */
    mutateInfoPowChallenge: (data?: HashPowChallenge | Promise<HashPowChallenge>, options?: MutatorOptions) =>
      mutate<HashPowChallenge>(`/api/captcha/powchallenge`, data, options),
  };
  proxy = {
    /**
     * No description
     *
     * @tags Proxy
     * @name ProxyProxyForInstance
     * @summary Proxy TCP over websocket
     * @request GET:/api/proxy/{id}
     */
    proxyProxyForInstance: (id: string, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/proxy/${id}`,
        method: "GET",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Proxy
     * @name ProxyProxyForNoInstance
     * @summary Proxy TCP over websocket for admins
     * @request GET:/api/proxy/noinst/{id}
     */
    proxyProxyForNoInstance: (id: string, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/proxy/noinst/${id}`,
        method: "GET",
        ...params,
      }),
  };
  team = {
    /**
     * @description Interface to accept invitation, requires User permission and not being in team
     *
     * @tags Team
     * @name TeamAccept
     * @summary Accept invitation
     * @request POST:/api/team/accept
     */
    teamAccept: (data: string, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/team/accept`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        ...params,
      }),

    /**
     * @description Use this API to update team avatar, requires User permission and team membership
     *
     * @tags Team
     * @name TeamAvatar
     * @summary Update team avatar
     * @request PUT:/api/team/{id}/avatar
     */
    teamAvatar: (
      id: number,
      data: {
        /** @format binary */
        file?: File | null;
      },
      params: RequestParams = {},
    ) =>
      this.request<string, RequestResponse>({
        path: `/api/team/${id}/avatar`,
        method: "PUT",
        body: data,
        type: ContentType.FormData,
        format: "json",
        ...params,
      }),

    /**
     * @description User API for creating teams, each user can only create one team
     *
     * @tags Team
     * @name TeamCreateTeam
     * @summary Create team
     * @request POST:/api/team
     */
    teamCreateTeam: (data: TeamUpdateModel, params: RequestParams = {}) =>
      this.request<TeamInfoModel, RequestResponse>({
        path: `/api/team`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * @description User API for deleting team, requires User permission and team captain status
     *
     * @tags Team
     * @name TeamDeleteTeam
     * @summary Delete team
     * @request DELETE:/api/team/{id}
     */
    teamDeleteTeam: (id: number, params: RequestParams = {}) =>
      this.request<TeamInfoModel, RequestResponse>({
        path: `/api/team/${id}`,
        method: "DELETE",
        format: "json",
        ...params,
      }),

    /**
     * @description Get basic information of a team by ID
     *
     * @tags Team
     * @name TeamGetBasicInfo
     * @summary Get team information
     * @request GET:/api/team/{id}
     */
    teamGetBasicInfo: (id: number, params: RequestParams = {}) =>
      this.request<TeamInfoModel, RequestResponse>({
        path: `/api/team/${id}`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * @description Get basic information of a team by ID
     *
     * @tags Team
     * @name TeamGetBasicInfo
     * @summary Get team information
     * @request GET:/api/team/{id}
     */
    useTeamGetBasicInfo: (id: number, options?: SWRConfiguration, doFetch: boolean = true) =>
      useSWR<TeamInfoModel, RequestResponse>(doFetch ? `/api/team/${id}` : null, options),

    /**
     * @description Get basic information of a team by ID
     *
     * @tags Team
     * @name TeamGetBasicInfo
     * @summary Get team information
     * @request GET:/api/team/{id}
     */
    mutateTeamGetBasicInfo: (id: number, data?: TeamInfoModel | Promise<TeamInfoModel>, options?: MutatorOptions) =>
      mutate<TeamInfoModel>(`/api/team/${id}`, data, options),

    /**
     * @description Get basic information of a team based on user
     *
     * @tags Team
     * @name TeamGetTeamsInfo
     * @summary Get current team information
     * @request GET:/api/team
     */
    teamGetTeamsInfo: (params: RequestParams = {}) =>
      this.request<TeamInfoModel[], RequestResponse>({
        path: `/api/team`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * @description Get basic information of a team based on user
     *
     * @tags Team
     * @name TeamGetTeamsInfo
     * @summary Get current team information
     * @request GET:/api/team
     */
    useTeamGetTeamsInfo: (options?: SWRConfiguration, doFetch: boolean = true) =>
      useSWR<TeamInfoModel[], RequestResponse>(doFetch ? `/api/team` : null, options),

    /**
     * @description Get basic information of a team based on user
     *
     * @tags Team
     * @name TeamGetTeamsInfo
     * @summary Get current team information
     * @request GET:/api/team
     */
    mutateTeamGetTeamsInfo: (data?: TeamInfoModel[] | Promise<TeamInfoModel[]>, options?: MutatorOptions) =>
      mutate<TeamInfoModel[]>(`/api/team`, data, options),

    /**
     * @description Get team invitation information, must be team creator
     *
     * @tags Team
     * @name TeamInviteCode
     * @summary Get invitation information
     * @request GET:/api/team/{id}/invite
     */
    teamInviteCode: (id: number, params: RequestParams = {}) =>
      this.request<string, RequestResponse>({
        path: `/api/team/${id}/invite`,
        method: "GET",
        format: "json",
        ...params,
      }),
    /**
     * @description Get team invitation information, must be team creator
     *
     * @tags Team
     * @name TeamInviteCode
     * @summary Get invitation information
     * @request GET:/api/team/{id}/invite
     */
    useTeamInviteCode: (id: number, options?: SWRConfiguration, doFetch: boolean = true) =>
      useSWR<string, RequestResponse>(doFetch ? `/api/team/${id}/invite` : null, options),

    /**
     * @description Get team invitation information, must be team creator
     *
     * @tags Team
     * @name TeamInviteCode
     * @summary Get invitation information
     * @request GET:/api/team/{id}/invite
     */
    mutateTeamInviteCode: (id: number, data?: string | Promise<string>, options?: MutatorOptions) =>
      mutate<string>(`/api/team/${id}/invite`, data, options),

    /**
     * @description User kick API, kick user with corresponding ID, requires team creator permission
     *
     * @tags Team
     * @name TeamKickUser
     * @summary Kick user
     * @request POST:/api/team/{id}/kick/{userId}
     */
    teamKickUser: (id: number, userId: string, params: RequestParams = {}) =>
      this.request<TeamInfoModel, RequestResponse>({
        path: `/api/team/${id}/kick/${userId}`,
        method: "POST",
        format: "json",
        ...params,
      }),

    /**
     * @description Interface to leave team, requires User permission and being in team
     *
     * @tags Team
     * @name TeamLeave
     * @summary Leave team
     * @request POST:/api/team/{id}/leave
     */
    teamLeave: (id: number, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/team/${id}/leave`,
        method: "POST",
        ...params,
      }),

    /**
     * @description Team ownership transfer API, must be team creator
     *
     * @tags Team
     * @name TeamTransfer
     * @summary Transfer team ownership
     * @request PUT:/api/team/{id}/transfer
     */
    teamTransfer: (id: number, data: TeamTransferModel, params: RequestParams = {}) =>
      this.request<TeamInfoModel, RequestResponse>({
        path: `/api/team/${id}/transfer`,
        method: "PUT",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * @description Interface to update invitation token, must be team creator
     *
     * @tags Team
     * @name TeamUpdateInviteToken
     * @summary Update invitation token
     * @request PUT:/api/team/{id}/invite
     */
    teamUpdateInviteToken: (id: number, params: RequestParams = {}) =>
      this.request<string, RequestResponse>({
        path: `/api/team/${id}/invite`,
        method: "PUT",
        format: "json",
        ...params,
      }),

    /**
     * @description Team information update API, must be team creator
     *
     * @tags Team
     * @name TeamUpdateTeam
     * @summary Update team information
     * @request PUT:/api/team/{id}
     */
    teamUpdateTeam: (id: number, data: TeamUpdateModel, params: RequestParams = {}) =>
      this.request<TeamInfoModel, RequestResponse>({
        path: `/api/team/${id}`,
        method: "PUT",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * @description Perform signature verification
     *
     * @tags Team
     * @name TeamVerifySignature
     * @summary Verify signature
     * @request POST:/api/team/verify
     */
    teamVerifySignature: (data: SignatureVerifyModel, params: RequestParams = {}) =>
      this.request<void, RequestResponse>({
        path: `/api/team/verify`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        ...params,
      }),
  };
}

const api = new Api();
export default api;

export const fetcher = async (args: string | [string, Record<string, unknown>]) => {
  if (typeof args === "string") {
    const response = await api.request({ path: args });
    return response.data;
  } else {
    const [path, query] = args;
    const response = await api.request({ path, query });
    return response.data;
  }
};
