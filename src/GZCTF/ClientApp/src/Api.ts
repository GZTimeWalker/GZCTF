/* eslint-disable */
/* tslint:disable */
// @ts-nocheck
/*
 * ---------------------------------------------------------------
 * ## THIS FILE WAS GENERATED VIA SWAGGER-TYPESCRIPT-API        ##
 * ##                                                           ##
 * ## AUTHOR: acacode                                           ##
 * ## SOURCE: https://github.com/acacode/swagger-typescript-api ##
 * ---------------------------------------------------------------
 */

export enum CaptchaProvider {
  None = "None",
  HashPow = "HashPow",
  CloudflareTurnstile = "CloudflareTurnstile",
}

export enum ContainerPortMappingType {
  Default = "Default",
  PlatformProxy = "PlatformProxy",
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

/** Task execution status */
export enum TaskStatus {
  Success = "Success",
  Failed = "Failed",
  Duplicate = "Duplicate",
  Denied = "Denied",
  NotFound = "NotFound",
  Exit = "Exit",
  Unhealthy = "Unhealthy",
  Degraded = "Degraded",
  Pending = "Pending",
}

/** User role enumeration */
export enum Role {
  Banned = "Banned",
  User = "User",
  Monitor = "Monitor",
  Admin = "Admin",
}

/** Login response status */
export enum RegisterStatus {
  LoggedIn = "LoggedIn",
  AdminConfirmationRequired = "AdminConfirmationRequired",
  EmailConfirmationRequired = "EmailConfirmationRequired",
}

export enum ChallengeFlagKind {
  Static = "Static",
  DynamicAttachment = "DynamicAttachment",
  Template = "Template",
}

export enum ChallengePublicationState {
  Draft = "Draft",
  Published = "Published",
  Retired = "Retired",
  Merged = "Merged",
}

/** Challenge difficulty */
export enum Difficulty {
  Baby = "Baby",
  Trivial = "Trivial",
  Easy = "Easy",
  Normal = "Normal",
  Medium = "Medium",
  Hard = "Hard",
  Expert = "Expert",
  Insane = "Insane",
}

export enum ChallengeType {
  StaticAttachment = "StaticAttachment",
  StaticContainer = "StaticContainer",
  DynamicAttachment = "DynamicAttachment",
  DynamicContainer = "DynamicContainer",
}

export enum ChallengeSolveMode {
  Independent = 0,
  AfterHint = 1,
  AfterWriteup = 2,
}

export enum ChallengeInstanceStatus {
  Pending = 0,
  Running = 1,
  Stopped = 2,
  Expired = 3,
  Failed = 4,
}

export enum MigrationBatchState {
  Pending = 0,
  Running = 1,
  Completed = 2,
  Failed = 3,
}

export interface SkillCategoryAdminResponse {
  /** @format guid */
  categoryId?: string;
  name?: string;
  summary?: string;
  iconKey?: string;
  rowVersion?: number;
  trees?: CategoryTreeReferenceResponse[];
  contents?: SkillTreeContentSummaryResponse[];
}

export interface CategoryTreeReferenceResponse {
  /** @format guid */
  skillTreeId?: string;
  name?: string;
  isPublished?: boolean;
}

export interface SkillTreeContentSummaryResponse {
  /** @format guid */
  contentId?: string;
  kind?: string;
  /** @format int32 */
  sortOrder?: number;
  title?: string;
  summary?: string;
  /** @format int32 */
  expectedMinutes?: number;
  difficulty?: string;
  state?: string | null;
}

export interface SkillCategoryCommand {
  name?: string;
  summary?: string;
  iconKey?: string;
  rowVersion?: number | null;
}

export interface UpdateCategoryContentsCommand {
  rowVersion?: number;
  contents?: CategoryContentOrderCommand[];
}

export interface CategoryContentOrderCommand {
  kind?: string;
  /** @format guid */
  contentId?: string;
  /** @format int32 */
  sortOrder?: number;
}

export interface UpdateCategoryTreeMembershipsResponse {
  /** @format guid */
  categoryId?: string;
  affectedSkillTreeIds?: string[];
}

export interface UpdateCategoryTreeMembershipsCommand {
  categoryRowVersion?: number;
  trees?: CategoryTreeMembershipCommand[];
}

export interface CategoryTreeMembershipCommand {
  /** @format guid */
  skillTreeId?: string;
  included?: boolean;
  skillTreeRowVersion?: number;
}

export interface CategoryDeleteImpactResponse {
  /** @format guid */
  categoryId?: string;
  name?: string;
  /** @format int32 */
  draftTreeCount?: number;
  /** @format int32 */
  publishedTreeCount?: number;
  /** @format int32 */
  challengeCount?: number;
  /** @format int32 */
  lessonCount?: number;
  requiresTypedConfirmation?: boolean;
}

export interface DeleteCategoryCommand {
  confirmationName?: string;
  rowVersion?: number;
}

export interface MergeSkillCategoryCommand {
  /** @format guid */
  survivorCategoryId?: string;
  /** @format guid */
  duplicateCategoryId?: string;
  survivorRowVersion?: number;
  duplicateRowVersion?: number;
}

export interface AdminSkillTreeResponse {
  /** @format guid */
  skillTreeId?: string;
  name?: string;
  summary?: string;
  iconKey?: string;
  isPublished?: boolean;
  hasDraft?: boolean;
  rowVersion?: number;
}

export interface CreateSkillTreeCommand {
  name?: string;
  summary?: string;
  iconKey?: string;
}

export interface SkillTreeDraftResponse {
  /** @format guid */
  skillTreeId?: string;
  /** @format guid */
  revisionId?: string;
  name?: string;
  summary?: string;
  iconKey?: string;
  rowVersion?: number;
  categories?: SkillTreeCategoryAdminResponse[];
}

export interface SkillTreeCategoryAdminResponse {
  /** @format guid */
  categoryId?: string;
  name?: string;
  summary?: string;
  iconKey?: string;
  /** @format int32 */
  sortOrder?: number;
  rowVersion?: number;
  trees?: CategoryTreeReferenceResponse[];
  contents?: SkillTreeContentSummaryResponse[];
}

export interface SkillTreeDetailResponse {
  /** @format guid */
  skillTreeId?: string;
  name?: string;
  summary?: string;
  iconKey?: string;
  categories?: SkillCategoryPublicResponse[];
}

export interface SkillCategoryPublicResponse {
  /** @format guid */
  categoryId?: string;
  name?: string;
  summary?: string;
  iconKey?: string;
  /** @format int32 */
  sortOrder?: number;
  contents?: SkillTreeContentSummaryResponse[];
}

export interface UpdateSkillTreeDraftCommand {
  name?: string;
  summary?: string;
  iconKey?: string;
  rowVersion?: number;
  categories?: SkillTreeCategoryOrderCommand[];
}

export interface SkillTreeCategoryOrderCommand {
  /** @format guid */
  categoryId?: string;
  /** @format int32 */
  sortOrder?: number;
}

export interface PublishSkillTreeCommand {
  rowVersion?: number;
}

export interface SkillTreeDeleteImpactResponse {
  /** @format guid */
  skillTreeId?: string;
  name?: string;
  /** @format int32 */
  categoryCount?: number;
  /** @format int32 */
  challengeCount?: number;
  /** @format int32 */
  lessonCount?: number;
  /** @format int32 */
  enrollmentCount?: number;
  isPublished?: boolean;
  requiresTypedConfirmation?: boolean;
  rowVersion?: number;
}

export interface DeleteSkillTreeCommand {
  confirmationName?: string;
  rowVersion?: number;
}

export interface LearningRedirectResponse {
  targetPath?: string;
}

export interface SkillTreeEnrollmentResponse {
  /** @format guid */
  enrollmentId?: string;
  /** @format guid */
  skillTreeId?: string;
  name?: string;
  iconKey?: string;
  isCurrent?: boolean;
  /** @format uint64 */
  enrolledAtUtc?: number;
}

export interface SkillTreeSummaryResponse {
  /** @format guid */
  skillTreeId?: string;
  name?: string;
  summary?: string;
  iconKey?: string;
  /** @format int32 */
  categoryCount?: number;
  /** @format int32 */
  challengeCount?: number;
  /** @format int32 */
  lessonCount?: number;
}

export interface MyLearningResponse {
  routes?: LearningRouteRecord[];
  /** @format guid */
  currentPathId?: string | null;
  completedLessonIds?: string[];
  solvedChallenges?: LearningChallengeRecord[];
  /** @format int32 */
  solvedChallengeCount?: number;
  recentActivity?: LearningActivityRecord[];
  /** @format guid */
  currentSkillTreeId?: string | null;
  skillTrees?: MySkillTreeRecordResponse[];
}

export interface LearningRouteRecord {
  /** @format guid */
  pathId?: string;
  slug?: string;
  title?: string;
  isCurrent?: boolean;
  /** @format double */
  progressPercent?: number;
  /** @format int32 */
  completedModules?: number;
  /** @format int32 */
  totalModules?: number;
  /** @format int32 */
  completedLessons?: number;
  /** @format int32 */
  totalLessons?: number;
  /** @format int32 */
  completedItems?: number;
  /** @format int32 */
  totalItems?: number;
  modules?: LearningModuleRecord[];
}

export interface LearningModuleRecord {
  /** @format guid */
  moduleId?: string;
  title?: string;
  /** @format double */
  progressPercent?: number;
  /** @format int32 */
  completedItems?: number;
  /** @format int32 */
  totalItems?: number;
  isComplete?: boolean;
}

export interface LearningChallengeRecord {
  /** @format guid */
  challengeId?: string;
  /** @format uint64 */
  solvedAtUtc?: number;
  solveMode?: string;
}

export interface LearningActivityRecord {
  kind?: string;
  /** @format guid */
  contentId?: string;
  /** @format uint64 */
  completedAtUtc?: number;
  solveMode?: string | null;
  title?: string | null;
}

export interface MySkillTreeRecordResponse {
  /** @format guid */
  skillTreeId?: string;
  name?: string;
  iconKey?: string;
  isCurrent?: boolean;
  isDeleted?: boolean;
  /** @format int32 */
  categoryCount?: number;
  /** @format int32 */
  completedCategoryCount?: number;
  /** @format int32 */
  challengeCount?: number;
  /** @format int32 */
  completedChallengeCount?: number;
  /** @format int32 */
  lessonCount?: number;
  /** @format int32 */
  completedLessonCount?: number;
}

export interface LessonContentResponse {
  /** @format guid */
  lessonId?: string;
  locale?: string;
  title?: string;
  body?: string;
}

export interface ImportBatchResponse {
  /** @format guid */
  id?: string;
  sourceType?: string;
  state?: MigrationBatchState;
  /** @format int32 */
  challengeCount?: number;
  /** @format int32 */
  pathCount?: number;
  /** @format int32 */
  warningCount?: number;
  /** @format int32 */
  errorCount?: number;
  parityReportJson?: string | null;
  /** @format uint64 */
  startedAtUtc?: number;
  /** @format uint64 */
  completedAtUtc?: number | null;
}

export interface CanonicalImportResult {
  /** @format guid */
  batchId?: string;
  state?: MigrationBatchState;
  /** @format int32 */
  challengeCount?: number;
  /** @format int32 */
  pathCount?: number;
  /** @format int32 */
  warningCount?: number;
}

export interface CohortResponse {
  /** @format guid */
  id?: string;
  name?: string;
  isActive?: boolean;
  /** @format int32 */
  memberCount?: number;
}

export interface CohortCommand {
  name?: string;
}

export interface CohortStatusCommand {
  isActive?: boolean;
}

export interface CohortMemberResponse {
  /** @format guid */
  id?: string;
  userName?: string;
}

export interface CohortMembersCommand {
  userIds?: string[];
}

export interface DailySolveRebuildResult {
  /** @format int32 */
  insertedRows?: number;
  /** @format uint64 */
  maximumSourceTimestamp?: number | null;
}

export interface AdminDashboardResponse {
  /** @format guid */
  id?: string;
  name?: string;
  /** @format int32 */
  topCount?: number;
  isEnabled?: boolean;
  /** @format int32 */
  activeTokenCount?: number;
}

export interface DashboardCommand {
  name?: string;
  /** @format int32 */
  topCount?: number;
}

export interface DashboardTokenResult {
  /** @format guid */
  tokenId?: string;
  rawToken?: string;
  /** @format uint64 */
  expiresAtUtc?: number | null;
}

export interface TokenExpiryCommand {
  /** @format uint64 */
  expiresAtUtc?: number | null;
}

export interface DashboardTokenSummaryResponse {
  /** @format guid */
  tokenId?: string;
  /** @format uint64 */
  createdAtUtc?: number;
  /** @format uint64 */
  expiresAtUtc?: number | null;
  /** @format uint64 */
  lastUsedAtUtc?: number | null;
}

export interface DashboardSnapshotResponse {
  /** @format guid */
  dashboardId?: string;
  /** @format uint64 */
  generatedAtUtc?: number;
  cohorts?: DashboardCohortChoice[];
  members?: DashboardMemberSeries[];
  leaderboard?: DashboardLeaderboardEntry[];
}

export interface DashboardCohortChoice {
  /** @format guid */
  id?: string;
  name?: string;
}

export interface DashboardMemberSeries {
  userName?: string;
  /** @format guid */
  cohortId?: string | null;
  points?: DashboardPoint[];
  /** @format int32 */
  uniqueSolvedCount?: number;
}

export interface DashboardPoint {
  /** @format date */
  date?: string;
  /** @format int32 */
  value?: number;
}

export interface DashboardLeaderboardEntry {
  userName?: string;
  /** @format int32 */
  uniqueSolvedCount?: number;
  /** @format int32 */
  rank?: number;
}

export interface ChallengeInstanceResponse {
  /** @format guid */
  id?: string;
  status?: ChallengeInstanceStatus;
  /** @format uint64 */
  startedAtUtc?: number | null;
  /** @format uint64 */
  expiresAtUtc?: number | null;
  publicIp?: string | null;
  /** @format int32 */
  publicPort?: number | null;
  attachmentFileName?: string | null;
  attachmentSha256?: string | null;
}

export interface ChallengeRuntimeDetailResponse {
  /** @format guid */
  id?: string;
  title?: string;
  summary?: string;
  body?: string;
  type?: string;
  /** @format int32 */
  hintLocaleCount?: number;
  hasWriteup?: boolean;
  hasAttachment?: boolean;
  hasContainer?: boolean;
}

export interface ChallengeHintResponse {
  /** @format guid */
  hintId?: string;
  /** @format int32 */
  sortOrder?: number;
  locale?: string;
  content?: string;
}

export interface ChallengeWriteupResponse {
  locale?: string;
  content?: string;
  /** @format uint64 */
  firstViewedAtUtc?: number;
}

export interface ChallengeSubmissionResult {
  accepted?: boolean;
  firstSolve?: boolean;
  solveMode?: ChallengeSolveMode | null;
  rejectionCode?: string | null;
  /** @format guid */
  submissionId?: string;
}

export interface ChallengeSubmissionRequest {
  flag?: string;
}

export interface ChallengeSummaryResponse {
  /** @format guid */
  id?: string;
  type?: ChallengeType;
  /** Challenge difficulty */
  difficulty?: Difficulty;
  publicationState?: ChallengePublicationState;
  isEnabled?: boolean;
  title?: string;
  summary?: string;
  sourceType?: string;
  sourceId?: string;
  sourceName?: string | null;
}

export interface ChallengeEditResponse {
  challenge?: ChallengeSummaryResponse;
  runtimeConfigurationJson?: string | null;
  localizations?: ChallengeLocalizationResponse[];
  flags?: ChallengeFlagResponse[];
  hints?: ChallengeHintResponse2[];
  writeups?: ChallengeWriteupResponse2[];
  publication?: ChallengePublicationEditState;
}

export interface ChallengeLocalizationResponse {
  locale?: string;
  title?: string;
  summary?: string;
  body?: string;
}

export interface ChallengeFlagResponse {
  /** @format guid */
  id?: string;
  kind?: ChallengeFlagKind;
  value?: string | null;
  template?: string | null;
  attachmentPoolKey?: string | null;
  metadataJson?: string | null;
}

export interface ChallengeHintResponse2 {
  /** @format guid */
  id?: string;
  locale?: string;
  /** @format int32 */
  sortOrder?: number;
  content?: string;
}

export interface ChallengeWriteupResponse2 {
  /** @format guid */
  id?: string;
  locale?: string;
  content?: string;
}

export interface ChallengePublicationEditState {
  rowVersion?: number;
  publicationState?: string;
  categoryIds?: string[];
}

export interface ChallengeCommand {
  type?: ChallengeType | null;
  difficulty?: Difficulty | null;
  sourceType?: string | null;
  sourceId?: string | null;
  sourceName?: string | null;
  sourceMetadataJson?: string | null;
  runtimeConfigurationJson?: string | null;
  isEnabled?: boolean | null;
  /** @format int32 */
  expectedMinutes?: number | null;
  locale?: string | null;
  localizations?: ChallengeLocalizationCommand[] | null;
  flags?: ChallengeFlagCommand[] | null;
  hints?: ChallengeHintCommand[] | null;
  writeups?: ChallengeWriteupCommand[] | null;
}

export interface ChallengeLocalizationCommand {
  locale?: string;
  title?: string;
  summary?: string;
  body?: string;
}

export interface ChallengeFlagCommand {
  kind?: ChallengeFlagKind;
  value?: string | null;
  template?: string | null;
  attachmentPoolKey?: string | null;
  metadataJson?: string | null;
}

export interface ChallengeHintCommand {
  locale?: string;
  /** @format int32 */
  sortOrder?: number;
  content?: string;
}

export interface ChallengeWriteupCommand {
  locale?: string;
  content?: string;
}

export interface PublishContentCommand {
  rowVersion?: number;
  categoryIds?: string[];
  inlineCategories?: InlineSkillCategoryCommand[];
}

export interface InlineSkillCategoryCommand {
  /** @format guid */
  skillTreeId?: string;
  name?: string;
  summary?: string;
  iconKey?: string;
}

export interface ChallengeMergeResult {
  /** @format guid */
  duplicateId?: string;
  /** @format guid */
  survivorId?: string;
  /** @format int32 */
  progressRowsReconciled?: number;
}

export interface MergeChallengeCommand {
  /** @format guid */
  survivorId?: string;
}

export interface LessonResponse {
  /** @format guid */
  id?: string;
  locale?: string;
  title?: string;
  body?: string;
  localizations?: LessonLocalizationResponse[];
  publication?: LessonPublicationEditState;
}

export interface LessonLocalizationResponse {
  locale?: string;
  title?: string;
  body?: string;
}

export interface LessonPublicationEditState {
  rowVersion?: number;
  publicationState?: string;
  categoryIds?: string[];
}

export interface LessonCommand {
  locale?: string | null;
  localizations?: LessonLocalizationCommand[] | null;
}

export interface LessonLocalizationCommand {
  locale?: string;
  title?: string;
  body?: string;
}

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
  userId?: string;
  /** User role */
  role?: Role;
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
  /** Use asymmetric encryption for API requests */
  apiEncryption?: boolean;
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
  status?: TaskStatus | null;
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

/** This record represents the response for an API token request. */
export interface ApiTokenResponse {
  token?: string;
  /** Represents an API token for programmatic access. */
  info?: ApiToken;
}

/** Represents an API token for programmatic access. */
export interface ApiToken {
  /**
   * The unique identifier for the token, also used as the JWT ID (jti).
   * @format guid
   */
  id?: string;
  /**
   * A user-friendly name for the token to identify its purpose.
   * @minLength 1
   * @maxLength 128
   */
  name: string;
  /**
   * The ID of the user who created the token.
   * @format guid
   * @minLength 1
   */
  creatorId: string;
  /**
   * The timestamp when the token was created.
   * @format uint64
   */
  createdAt: number;
  /**
   * The timestamp when the token expires. A null value means it never expires.
   * @format uint64
   */
  expiresAt?: number | null;
  /**
   * The timestamp when the token was last used.
   * @format uint64
   */
  lastUsedAt?: number | null;
  /** Indicates whether the token has been revoked. */
  isRevoked: boolean;
  /** The name of the user who created the token. */
  creator?: string | null;
}

/** API token creation model. */
export interface ApiTokenCreateModel {
  /**
   * The user-friendly name for the token to identify its purpose.
   * @minLength 1
   * @maxLength 128
   */
  name: string;
  /** The duration for which the token will be valid, in days. */
  expiresIn?: number | null;
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
   * @maxLength 50
   */
  title?: string | null;
  /** Post summary */
  summary?: string | null;
  /** Post content */
  content?: string | null;
  /** Post tags */
  tags?: string[] | null;
  /** Is pinned */
  isPinned?: boolean | null;
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
   */
  time: number;
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
  /** The public key used for API requests */
  apiPublicKey?: string | null;
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

/** Client CAPTCHA information */
export interface ClientCaptchaInfoModel {
  /** Captcha Provider Type */
  type?: CaptchaProvider;
  /** Site Key */
  siteKey?: string;
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

import { apiLanguage } from "@Utils/I18n";
import type {
  AxiosInstance,
  AxiosRequestConfig,
  AxiosResponse,
  HeadersDefaults,
  ResponseType,
} from "axios";
import axios from "axios";

export type QueryParamsType = Record<string | number, any>;

export interface FullRequestParams
  extends Omit<AxiosRequestConfig, "data" | "params" | "url" | "responseType"> {
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

export type RequestParams = Omit<
  FullRequestParams,
  "body" | "method" | "query" | "path"
>;

export interface ApiConfig<SecurityDataType = unknown>
  extends Omit<AxiosRequestConfig, "data" | "cancelToken"> {
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

  constructor({
    securityWorker,
    secure,
    format,
    ...axiosConfig
  }: ApiConfig<SecurityDataType> = {}) {
    this.instance = axios.create({
      ...axiosConfig,
      baseURL: axiosConfig.baseURL || "",
    });
    this.secure = secure;
    this.format = format;
    this.securityWorker = securityWorker;
  }

  public setSecurityData = (data: SecurityDataType | null) => {
    this.securityData = data;
  };

  protected mergeRequestParams(
    params1: AxiosRequestConfig,
    params2?: AxiosRequestConfig,
  ): AxiosRequestConfig {
    const method = params1.method || (params2 && params2.method);

    return {
      ...this.instance.defaults,
      ...params1,
      ...params2,
      headers: {
        ...(method &&
          this.instance.defaults.headers[
            method.toLowerCase() as keyof HeadersDefaults
          ]),
        ...params1.headers,
        ...(params2 && params2.headers),
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
      const propertyContent: any[] =
        property instanceof Array ? property : [property];

      for (const formItem of propertyContent) {
        const isFileType = formItem instanceof Blob || formItem instanceof File;
        formData.append(
          key,
          isFileType ? formItem : this.stringifyFormItem(formItem),
        );
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

    if (
      type === ContentType.FormData &&
      body &&
      body !== null &&
      typeof body === "object"
    ) {
      body = this.createFormData(body as Record<string, unknown>);
    }

    if (
      type === ContentType.Text &&
      body &&
      body !== null &&
      typeof body !== "string"
    ) {
      body = JSON.stringify(body);
    }

    return this.instance.request({
      ...requestParams,
      headers: {
        ...requestParams.headers,
        ...(type && type !== ContentType.FormData
          ? { "Content-Type": type }
          : {}),
        "Accept-Language": apiLanguage,
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
export class Api<
  SecurityDataType extends unknown,
> extends HttpClient<SecurityDataType> {
  adminSkillCategories = {
    /**
     * No description
     *
     * @tags AdminSkillCategories
     * @name AdminSkillCategoriesCreate
     * @request POST:/api/admin/skill-categories
     */
    adminSkillCategoriesCreate: (
      data: SkillCategoryCommand,
      params: RequestParams = {},
    ) =>
      this.request<SkillCategoryAdminResponse, any>({
        path: `/api/admin/skill-categories`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags AdminSkillCategories
     * @name AdminSkillCategoriesDelete
     * @request DELETE:/api/admin/skill-categories/{id}
     */
    adminSkillCategoriesDelete: (
      id: string,
      data: DeleteCategoryCommand,
      params: RequestParams = {},
    ) =>
      this.request<Blob, any>({
        path: `/api/admin/skill-categories/${id}`,
        method: "DELETE",
        body: data,
        type: ContentType.Json,
        ...params,
      }),

    /**
     * No description
     *
     * @tags AdminSkillCategories
     * @name AdminSkillCategoriesGet
     * @request GET:/api/admin/skill-categories/{id}
     */
    adminSkillCategoriesGet: (id: string, params: RequestParams = {}) =>
      this.request<SkillCategoryAdminResponse, any>({
        path: `/api/admin/skill-categories/${id}`,
        method: "GET",
        format: "json",
        ...params,
      }),
    useAdminSkillCategoriesGet: (
      id: string,
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<SkillCategoryAdminResponse, any>(
        doFetch ? `/api/admin/skill-categories/${id}` : null,
        options,
      ),

    mutateAdminSkillCategoriesGet: (
      id: string,
      data?: SkillCategoryAdminResponse | Promise<SkillCategoryAdminResponse>,
      options?: MutatorOptions,
    ) =>
      mutate<SkillCategoryAdminResponse>(
        `/api/admin/skill-categories/${id}`,
        data,
        options,
      ),

    /**
     * No description
     *
     * @tags AdminSkillCategories
     * @name AdminSkillCategoriesGetDeleteImpact
     * @request GET:/api/admin/skill-categories/{id}/delete-impact
     */
    adminSkillCategoriesGetDeleteImpact: (
      id: string,
      params: RequestParams = {},
    ) =>
      this.request<CategoryDeleteImpactResponse, any>({
        path: `/api/admin/skill-categories/${id}/delete-impact`,
        method: "GET",
        format: "json",
        ...params,
      }),
    useAdminSkillCategoriesGetDeleteImpact: (
      id: string,
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<CategoryDeleteImpactResponse, any>(
        doFetch ? `/api/admin/skill-categories/${id}/delete-impact` : null,
        options,
      ),

    mutateAdminSkillCategoriesGetDeleteImpact: (
      id: string,
      data?:
        | CategoryDeleteImpactResponse
        | Promise<CategoryDeleteImpactResponse>,
      options?: MutatorOptions,
    ) =>
      mutate<CategoryDeleteImpactResponse>(
        `/api/admin/skill-categories/${id}/delete-impact`,
        data,
        options,
      ),

    /**
     * No description
     *
     * @tags AdminSkillCategories
     * @name AdminSkillCategoriesList
     * @request GET:/api/admin/skill-categories
     */
    adminSkillCategoriesList: (params: RequestParams = {}) =>
      this.request<SkillCategoryAdminResponse[], any>({
        path: `/api/admin/skill-categories`,
        method: "GET",
        format: "json",
        ...params,
      }),
    useAdminSkillCategoriesList: (
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<SkillCategoryAdminResponse[], any>(
        doFetch ? `/api/admin/skill-categories` : null,
        options,
      ),

    mutateAdminSkillCategoriesList: (
      data?:
        | SkillCategoryAdminResponse[]
        | Promise<SkillCategoryAdminResponse[]>,
      options?: MutatorOptions,
    ) =>
      mutate<SkillCategoryAdminResponse[]>(
        `/api/admin/skill-categories`,
        data,
        options,
      ),

    /**
     * No description
     *
     * @tags AdminSkillCategories
     * @name AdminSkillCategoriesMerge
     * @request POST:/api/admin/skill-categories/merge
     */
    adminSkillCategoriesMerge: (
      data: MergeSkillCategoryCommand,
      params: RequestParams = {},
    ) =>
      this.request<Blob, any>({
        path: `/api/admin/skill-categories/merge`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        ...params,
      }),

    /**
     * No description
     *
     * @tags AdminSkillCategories
     * @name AdminSkillCategoriesUpdate
     * @request PUT:/api/admin/skill-categories/{id}
     */
    adminSkillCategoriesUpdate: (
      id: string,
      data: SkillCategoryCommand,
      params: RequestParams = {},
    ) =>
      this.request<SkillCategoryAdminResponse, any>({
        path: `/api/admin/skill-categories/${id}`,
        method: "PUT",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags AdminSkillCategories
     * @name AdminSkillCategoriesUpdateContents
     * @request PUT:/api/admin/skill-categories/{id}/contents
     */
    adminSkillCategoriesUpdateContents: (
      id: string,
      data: UpdateCategoryContentsCommand,
      params: RequestParams = {},
    ) =>
      this.request<SkillCategoryAdminResponse, any>({
        path: `/api/admin/skill-categories/${id}/contents`,
        method: "PUT",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags AdminSkillCategories
     * @name AdminSkillCategoriesUpdateTreeMemberships
     * @request PUT:/api/admin/skill-categories/{id}/tree-memberships
     */
    adminSkillCategoriesUpdateTreeMemberships: (
      id: string,
      data: UpdateCategoryTreeMembershipsCommand,
      params: RequestParams = {},
    ) =>
      this.request<UpdateCategoryTreeMembershipsResponse, any>({
        path: `/api/admin/skill-categories/${id}/tree-memberships`,
        method: "PUT",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),
  };
  adminSkillTrees = {
    /**
     * No description
     *
     * @tags AdminSkillTrees
     * @name AdminSkillTreesCreate
     * @request POST:/api/admin/skill-trees
     */
    adminSkillTreesCreate: (
      data: CreateSkillTreeCommand,
      params: RequestParams = {},
    ) =>
      this.request<AdminSkillTreeResponse, any>({
        path: `/api/admin/skill-trees`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags AdminSkillTrees
     * @name AdminSkillTreesDelete
     * @request DELETE:/api/admin/skill-trees/{id}
     */
    adminSkillTreesDelete: (
      id: string,
      data: DeleteSkillTreeCommand,
      params: RequestParams = {},
    ) =>
      this.request<Blob, any>({
        path: `/api/admin/skill-trees/${id}`,
        method: "DELETE",
        body: data,
        type: ContentType.Json,
        ...params,
      }),

    /**
     * No description
     *
     * @tags AdminSkillTrees
     * @name AdminSkillTreesGetDeleteImpact
     * @request GET:/api/admin/skill-trees/{id}/delete-impact
     */
    adminSkillTreesGetDeleteImpact: (id: string, params: RequestParams = {}) =>
      this.request<SkillTreeDeleteImpactResponse, any>({
        path: `/api/admin/skill-trees/${id}/delete-impact`,
        method: "GET",
        format: "json",
        ...params,
      }),
    useAdminSkillTreesGetDeleteImpact: (
      id: string,
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<SkillTreeDeleteImpactResponse, any>(
        doFetch ? `/api/admin/skill-trees/${id}/delete-impact` : null,
        options,
      ),

    mutateAdminSkillTreesGetDeleteImpact: (
      id: string,
      data?:
        | SkillTreeDeleteImpactResponse
        | Promise<SkillTreeDeleteImpactResponse>,
      options?: MutatorOptions,
    ) =>
      mutate<SkillTreeDeleteImpactResponse>(
        `/api/admin/skill-trees/${id}/delete-impact`,
        data,
        options,
      ),

    /**
     * No description
     *
     * @tags AdminSkillTrees
     * @name AdminSkillTreesGetDraft
     * @request GET:/api/admin/skill-trees/{id}/draft
     */
    adminSkillTreesGetDraft: (id: string, params: RequestParams = {}) =>
      this.request<SkillTreeDraftResponse, any>({
        path: `/api/admin/skill-trees/${id}/draft`,
        method: "GET",
        format: "json",
        ...params,
      }),
    useAdminSkillTreesGetDraft: (
      id: string,
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<SkillTreeDraftResponse, any>(
        doFetch ? `/api/admin/skill-trees/${id}/draft` : null,
        options,
      ),

    mutateAdminSkillTreesGetDraft: (
      id: string,
      data?: SkillTreeDraftResponse | Promise<SkillTreeDraftResponse>,
      options?: MutatorOptions,
    ) =>
      mutate<SkillTreeDraftResponse>(
        `/api/admin/skill-trees/${id}/draft`,
        data,
        options,
      ),

    /**
     * No description
     *
     * @tags AdminSkillTrees
     * @name AdminSkillTreesList
     * @request GET:/api/admin/skill-trees
     */
    adminSkillTreesList: (params: RequestParams = {}) =>
      this.request<AdminSkillTreeResponse[], any>({
        path: `/api/admin/skill-trees`,
        method: "GET",
        format: "json",
        ...params,
      }),
    useAdminSkillTreesList: (
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<AdminSkillTreeResponse[], any>(
        doFetch ? `/api/admin/skill-trees` : null,
        options,
      ),

    mutateAdminSkillTreesList: (
      data?: AdminSkillTreeResponse[] | Promise<AdminSkillTreeResponse[]>,
      options?: MutatorOptions,
    ) =>
      mutate<AdminSkillTreeResponse[]>(`/api/admin/skill-trees`, data, options),

    /**
     * No description
     *
     * @tags AdminSkillTrees
     * @name AdminSkillTreesPreviewDraft
     * @request GET:/api/admin/skill-trees/{id}/draft/preview
     */
    adminSkillTreesPreviewDraft: (id: string, params: RequestParams = {}) =>
      this.request<SkillTreeDetailResponse, any>({
        path: `/api/admin/skill-trees/${id}/draft/preview`,
        method: "GET",
        format: "json",
        ...params,
      }),
    useAdminSkillTreesPreviewDraft: (
      id: string,
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<SkillTreeDetailResponse, any>(
        doFetch ? `/api/admin/skill-trees/${id}/draft/preview` : null,
        options,
      ),

    mutateAdminSkillTreesPreviewDraft: (
      id: string,
      data?: SkillTreeDetailResponse | Promise<SkillTreeDetailResponse>,
      options?: MutatorOptions,
    ) =>
      mutate<SkillTreeDetailResponse>(
        `/api/admin/skill-trees/${id}/draft/preview`,
        data,
        options,
      ),

    /**
     * No description
     *
     * @tags AdminSkillTrees
     * @name AdminSkillTreesPublish
     * @request POST:/api/admin/skill-trees/{id}/publish
     */
    adminSkillTreesPublish: (
      id: string,
      data: PublishSkillTreeCommand,
      params: RequestParams = {},
    ) =>
      this.request<Blob, any>({
        path: `/api/admin/skill-trees/${id}/publish`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        ...params,
      }),

    /**
     * No description
     *
     * @tags AdminSkillTrees
     * @name AdminSkillTreesUpdateDraft
     * @request PUT:/api/admin/skill-trees/{id}/draft
     */
    adminSkillTreesUpdateDraft: (
      id: string,
      data: UpdateSkillTreeDraftCommand,
      params: RequestParams = {},
    ) =>
      this.request<SkillTreeDraftResponse, any>({
        path: `/api/admin/skill-trees/${id}/draft`,
        method: "PUT",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),
  };
  learningRedirects = {
    /**
     * No description
     *
     * @tags LearningRedirects
     * @name LearningRedirectsGet
     * @request GET:/api/skill-tree-redirects/{slug}
     */
    learningRedirectsGet: (
      slug: string,
      query?: {
        moduleId?: string | null;
        itemId?: string | null;
      },
      params: RequestParams = {},
    ) =>
      this.request<LearningRedirectResponse, any>({
        path: `/api/skill-tree-redirects/${slug}`,
        method: "GET",
        query: query,
        format: "json",
        ...params,
      }),
    useLearningRedirectsGet: (
      slug: string,
      query?: {
        moduleId?: string | null;
        itemId?: string | null;
      },
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<LearningRedirectResponse, any>(
        doFetch ? [`/api/skill-tree-redirects/${slug}`, query] : null,
        options,
      ),

    mutateLearningRedirectsGet: (
      slug: string,
      query?: {
        moduleId?: string | null;
        itemId?: string | null;
      },
      data?: LearningRedirectResponse | Promise<LearningRedirectResponse>,
      options?: MutatorOptions,
    ) =>
      mutate<LearningRedirectResponse>(
        [`/api/skill-tree-redirects/${slug}`, query],
        data,
        options,
      ),
  };
  skillTreeEnrollments = {
    /**
     * No description
     *
     * @tags SkillTreeEnrollments
     * @name SkillTreeEnrollmentsEnroll
     * @request POST:/api/skill-tree-enrollments/{id}
     */
    skillTreeEnrollmentsEnroll: (id: string, params: RequestParams = {}) =>
      this.request<SkillTreeEnrollmentResponse, any>({
        path: `/api/skill-tree-enrollments/${id}`,
        method: "POST",
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags SkillTreeEnrollments
     * @name SkillTreeEnrollmentsLeave
     * @request DELETE:/api/skill-tree-enrollments/{id}
     */
    skillTreeEnrollmentsLeave: (id: string, params: RequestParams = {}) =>
      this.request<Blob, any>({
        path: `/api/skill-tree-enrollments/${id}`,
        method: "DELETE",
        ...params,
      }),

    /**
     * No description
     *
     * @tags SkillTreeEnrollments
     * @name SkillTreeEnrollmentsList
     * @request GET:/api/skill-tree-enrollments
     */
    skillTreeEnrollmentsList: (params: RequestParams = {}) =>
      this.request<SkillTreeEnrollmentResponse[], any>({
        path: `/api/skill-tree-enrollments`,
        method: "GET",
        format: "json",
        ...params,
      }),
    useSkillTreeEnrollmentsList: (
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<SkillTreeEnrollmentResponse[], any>(
        doFetch ? `/api/skill-tree-enrollments` : null,
        options,
      ),

    mutateSkillTreeEnrollmentsList: (
      data?:
        | SkillTreeEnrollmentResponse[]
        | Promise<SkillTreeEnrollmentResponse[]>,
      options?: MutatorOptions,
    ) =>
      mutate<SkillTreeEnrollmentResponse[]>(
        `/api/skill-tree-enrollments`,
        data,
        options,
      ),

    /**
     * No description
     *
     * @tags SkillTreeEnrollments
     * @name SkillTreeEnrollmentsSelectCurrent
     * @request PUT:/api/skill-tree-enrollments/{id}/current
     */
    skillTreeEnrollmentsSelectCurrent: (
      id: string,
      params: RequestParams = {},
    ) =>
      this.request<SkillTreeEnrollmentResponse, any>({
        path: `/api/skill-tree-enrollments/${id}/current`,
        method: "PUT",
        format: "json",
        ...params,
      }),
  };
  skillTrees = {
    /**
     * No description
     *
     * @tags SkillTrees
     * @name SkillTreesDetail
     * @request GET:/api/skill-trees/{id}
     */
    skillTreesDetail: (id: string, params: RequestParams = {}) =>
      this.request<SkillTreeDetailResponse, any>({
        path: `/api/skill-trees/${id}`,
        method: "GET",
        format: "json",
        ...params,
      }),
    useSkillTreesDetail: (
      id: string,
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<SkillTreeDetailResponse, any>(
        doFetch ? `/api/skill-trees/${id}` : null,
        options,
      ),

    mutateSkillTreesDetail: (
      id: string,
      data?: SkillTreeDetailResponse | Promise<SkillTreeDetailResponse>,
      options?: MutatorOptions,
    ) =>
      mutate<SkillTreeDetailResponse>(`/api/skill-trees/${id}`, data, options),

    /**
     * No description
     *
     * @tags SkillTrees
     * @name SkillTreesList
     * @request GET:/api/skill-trees
     */
    skillTreesList: (params: RequestParams = {}) =>
      this.request<SkillTreeSummaryResponse[], any>({
        path: `/api/skill-trees`,
        method: "GET",
        format: "json",
        ...params,
      }),
    useSkillTreesList: (options?: SWRConfiguration, doFetch: boolean = true) =>
      useSWR<SkillTreeSummaryResponse[], any>(
        doFetch ? `/api/skill-trees` : null,
        options,
      ),

    mutateSkillTreesList: (
      data?: SkillTreeSummaryResponse[] | Promise<SkillTreeSummaryResponse[]>,
      options?: MutatorOptions,
    ) => mutate<SkillTreeSummaryResponse[]>(`/api/skill-trees`, data, options),
  };
  myLearning = {
    /**
     * No description
     *
     * @tags MyLearning
     * @name MyLearningGet
     * @request GET:/api/my-learning
     */
    myLearningGet: (
      query?: {
        locale?: string | null;
      },
      params: RequestParams = {},
    ) =>
      this.request<MyLearningResponse, any>({
        path: `/api/my-learning`,
        method: "GET",
        query: query,
        format: "json",
        ...params,
      }),
    useMyLearningGet: (
      query?: {
        locale?: string | null;
      },
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<MyLearningResponse, any>(
        doFetch ? [`/api/my-learning`, query] : null,
        options,
      ),

    mutateMyLearningGet: (
      query?: {
        locale?: string | null;
      },
      data?: MyLearningResponse | Promise<MyLearningResponse>,
      options?: MutatorOptions,
    ) => mutate<MyLearningResponse>([`/api/my-learning`, query], data, options),
  };
  lessons = {
    /**
     * No description
     *
     * @tags Lessons
     * @name LessonsComplete
     * @request POST:/api/learning-lessons/{id}/complete
     */
    lessonsComplete: (id: string, params: RequestParams = {}) =>
      this.request<Blob, any>({
        path: `/api/learning-lessons/${id}/complete`,
        method: "POST",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Lessons
     * @name LessonsGet
     * @request GET:/api/learning-lessons/{id}
     */
    lessonsGet: (
      id: string,
      query?: {
        locale?: string | null;
      },
      params: RequestParams = {},
    ) =>
      this.request<LessonContentResponse, any>({
        path: `/api/learning-lessons/${id}`,
        method: "GET",
        query: query,
        format: "json",
        ...params,
      }),
    useLessonsGet: (
      id: string,
      query?: {
        locale?: string | null;
      },
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<LessonContentResponse, any>(
        doFetch ? [`/api/learning-lessons/${id}`, query] : null,
        options,
      ),

    mutateLessonsGet: (
      id: string,
      query?: {
        locale?: string | null;
      },
      data?: LessonContentResponse | Promise<LessonContentResponse>,
      options?: MutatorOptions,
    ) =>
      mutate<LessonContentResponse>(
        [`/api/learning-lessons/${id}`, query],
        data,
        options,
      ),
  };
  imports = {
    /**
     * No description
     *
     * @tags Imports
     * @name ImportsGet
     * @request GET:/api/admin/imports/{id}
     */
    importsGet: (id: string, params: RequestParams = {}) =>
      this.request<ImportBatchResponse, any>({
        path: `/api/admin/imports/${id}`,
        method: "GET",
        format: "json",
        ...params,
      }),
    useImportsGet: (
      id: string,
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<ImportBatchResponse, any>(
        doFetch ? `/api/admin/imports/${id}` : null,
        options,
      ),

    mutateImportsGet: (
      id: string,
      data?: ImportBatchResponse | Promise<ImportBatchResponse>,
      options?: MutatorOptions,
    ) => mutate<ImportBatchResponse>(`/api/admin/imports/${id}`, data, options),

    /**
     * No description
     *
     * @tags Imports
     * @name ImportsList
     * @request GET:/api/admin/imports
     */
    importsList: (params: RequestParams = {}) =>
      this.request<ImportBatchResponse[], any>({
        path: `/api/admin/imports`,
        method: "GET",
        format: "json",
        ...params,
      }),
    useImportsList: (options?: SWRConfiguration, doFetch: boolean = true) =>
      useSWR<ImportBatchResponse[], any>(
        doFetch ? `/api/admin/imports` : null,
        options,
      ),

    mutateImportsList: (
      data?: ImportBatchResponse[] | Promise<ImportBatchResponse[]>,
      options?: MutatorOptions,
    ) => mutate<ImportBatchResponse[]>(`/api/admin/imports`, data, options),

    /**
     * No description
     *
     * @tags Imports
     * @name ImportsUpload
     * @request POST:/api/admin/imports/zip
     */
    importsUpload: (
      data: {
        /** @format binary */
        package?: File | null;
      },
      params: RequestParams = {},
    ) =>
      this.request<CanonicalImportResult, any>({
        path: `/api/admin/imports/zip`,
        method: "POST",
        body: data,
        type: ContentType.FormData,
        format: "json",
        ...params,
      }),
  };
  adminCohorts = {
    /**
     * No description
     *
     * @tags AdminCohorts
     * @name AdminCohortsAssign
     * @request POST:/api/admin/cohorts/{id}/members
     */
    adminCohortsAssign: (
      id: string,
      data: CohortMembersCommand,
      params: RequestParams = {},
    ) =>
      this.request<Blob, any>({
        path: `/api/admin/cohorts/${id}/members`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        ...params,
      }),

    /**
     * No description
     *
     * @tags AdminCohorts
     * @name AdminCohortsClear
     * @request DELETE:/api/admin/cohorts/{id}/members/{userId}
     */
    adminCohortsClear: (
      id: string,
      userId: string,
      params: RequestParams = {},
    ) =>
      this.request<Blob, any>({
        path: `/api/admin/cohorts/${id}/members/${userId}`,
        method: "DELETE",
        ...params,
      }),

    /**
     * No description
     *
     * @tags AdminCohorts
     * @name AdminCohortsCreate
     * @request POST:/api/admin/cohorts
     */
    adminCohortsCreate: (data: CohortCommand, params: RequestParams = {}) =>
      this.request<CohortResponse, any>({
        path: `/api/admin/cohorts`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags AdminCohorts
     * @name AdminCohortsList
     * @request GET:/api/admin/cohorts
     */
    adminCohortsList: (params: RequestParams = {}) =>
      this.request<CohortResponse[], any>({
        path: `/api/admin/cohorts`,
        method: "GET",
        format: "json",
        ...params,
      }),
    useAdminCohortsList: (
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<CohortResponse[], any>(
        doFetch ? `/api/admin/cohorts` : null,
        options,
      ),

    mutateAdminCohortsList: (
      data?: CohortResponse[] | Promise<CohortResponse[]>,
      options?: MutatorOptions,
    ) => mutate<CohortResponse[]>(`/api/admin/cohorts`, data, options),

    /**
     * No description
     *
     * @tags AdminCohorts
     * @name AdminCohortsMembers
     * @request GET:/api/admin/cohorts/{id}/members
     */
    adminCohortsMembers: (
      id: string,
      query?: {
        search?: string | null;
      },
      params: RequestParams = {},
    ) =>
      this.request<CohortMemberResponse[], any>({
        path: `/api/admin/cohorts/${id}/members`,
        method: "GET",
        query: query,
        format: "json",
        ...params,
      }),
    useAdminCohortsMembers: (
      id: string,
      query?: {
        search?: string | null;
      },
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<CohortMemberResponse[], any>(
        doFetch ? [`/api/admin/cohorts/${id}/members`, query] : null,
        options,
      ),

    mutateAdminCohortsMembers: (
      id: string,
      query?: {
        search?: string | null;
      },
      data?: CohortMemberResponse[] | Promise<CohortMemberResponse[]>,
      options?: MutatorOptions,
    ) =>
      mutate<CohortMemberResponse[]>(
        [`/api/admin/cohorts/${id}/members`, query],
        data,
        options,
      ),

    /**
     * No description
     *
     * @tags AdminCohorts
     * @name AdminCohortsRename
     * @request PUT:/api/admin/cohorts/{id}
     */
    adminCohortsRename: (
      id: string,
      data: CohortCommand,
      params: RequestParams = {},
    ) =>
      this.request<Blob, any>({
        path: `/api/admin/cohorts/${id}`,
        method: "PUT",
        body: data,
        type: ContentType.Json,
        ...params,
      }),

    /**
     * No description
     *
     * @tags AdminCohorts
     * @name AdminCohortsSetStatus
     * @request POST:/api/admin/cohorts/{id}/status
     */
    adminCohortsSetStatus: (
      id: string,
      data: CohortStatusCommand,
      params: RequestParams = {},
    ) =>
      this.request<Blob, any>({
        path: `/api/admin/cohorts/${id}/status`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        ...params,
      }),
  };
  adminDashboardProjection = {
    /**
     * No description
     *
     * @tags AdminDashboardProjection
     * @name AdminDashboardProjectionRebuild
     * @request POST:/api/admin/dashboard/projection/rebuild
     */
    adminDashboardProjectionRebuild: (params: RequestParams = {}) =>
      this.request<DailySolveRebuildResult, any>({
        path: `/api/admin/dashboard/projection/rebuild`,
        method: "POST",
        format: "json",
        ...params,
      }),
  };
  adminDashboards = {
    /**
     * No description
     *
     * @tags AdminDashboards
     * @name AdminDashboardsCreate
     * @request POST:/api/admin/dashboards
     */
    adminDashboardsCreate: (
      data: DashboardCommand,
      params: RequestParams = {},
    ) =>
      this.request<AdminDashboardResponse, any>({
        path: `/api/admin/dashboards`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags AdminDashboards
     * @name AdminDashboardsCreateToken
     * @request POST:/api/admin/dashboards/{id}/tokens
     */
    adminDashboardsCreateToken: (
      id: string,
      data: TokenExpiryCommand,
      params: RequestParams = {},
    ) =>
      this.request<DashboardTokenResult, any>({
        path: `/api/admin/dashboards/${id}/tokens`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags AdminDashboards
     * @name AdminDashboardsList
     * @request GET:/api/admin/dashboards
     */
    adminDashboardsList: (params: RequestParams = {}) =>
      this.request<AdminDashboardResponse[], any>({
        path: `/api/admin/dashboards`,
        method: "GET",
        format: "json",
        ...params,
      }),
    useAdminDashboardsList: (
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<AdminDashboardResponse[], any>(
        doFetch ? `/api/admin/dashboards` : null,
        options,
      ),

    mutateAdminDashboardsList: (
      data?: AdminDashboardResponse[] | Promise<AdminDashboardResponse[]>,
      options?: MutatorOptions,
    ) =>
      mutate<AdminDashboardResponse[]>(`/api/admin/dashboards`, data, options),

    /**
     * No description
     *
     * @tags AdminDashboards
     * @name AdminDashboardsListTokens
     * @request GET:/api/admin/dashboards/{id}/tokens
     */
    adminDashboardsListTokens: (id: string, params: RequestParams = {}) =>
      this.request<DashboardTokenSummaryResponse[], any>({
        path: `/api/admin/dashboards/${id}/tokens`,
        method: "GET",
        format: "json",
        ...params,
      }),
    useAdminDashboardsListTokens: (
      id: string,
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<DashboardTokenSummaryResponse[], any>(
        doFetch ? `/api/admin/dashboards/${id}/tokens` : null,
        options,
      ),

    mutateAdminDashboardsListTokens: (
      id: string,
      data?:
        | DashboardTokenSummaryResponse[]
        | Promise<DashboardTokenSummaryResponse[]>,
      options?: MutatorOptions,
    ) =>
      mutate<DashboardTokenSummaryResponse[]>(
        `/api/admin/dashboards/${id}/tokens`,
        data,
        options,
      ),

    /**
     * No description
     *
     * @tags AdminDashboards
     * @name AdminDashboardsRevokeToken
     * @request POST:/api/admin/dashboards/{id}/tokens/{tokenId}/revoke
     */
    adminDashboardsRevokeToken: (
      id: string,
      tokenId: string,
      params: RequestParams = {},
    ) =>
      this.request<Blob, any>({
        path: `/api/admin/dashboards/${id}/tokens/${tokenId}/revoke`,
        method: "POST",
        ...params,
      }),

    /**
     * No description
     *
     * @tags AdminDashboards
     * @name AdminDashboardsRotateToken
     * @request POST:/api/admin/dashboards/{id}/tokens/{tokenId}/rotate
     */
    adminDashboardsRotateToken: (
      id: string,
      tokenId: string,
      data: TokenExpiryCommand,
      params: RequestParams = {},
    ) =>
      this.request<DashboardTokenResult, any>({
        path: `/api/admin/dashboards/${id}/tokens/${tokenId}/rotate`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags AdminDashboards
     * @name AdminDashboardsUpdate
     * @request PUT:/api/admin/dashboards/{id}
     */
    adminDashboardsUpdate: (
      id: string,
      data: DashboardCommand,
      params: RequestParams = {},
    ) =>
      this.request<Blob, any>({
        path: `/api/admin/dashboards/${id}`,
        method: "PUT",
        body: data,
        type: ContentType.Json,
        ...params,
      }),
  };
  dashboards = {
    /**
     * No description
     *
     * @tags Dashboards
     * @name DashboardsGet
     * @request GET:/api/dashboards/{id}
     */
    dashboardsGet: (
      id: string,
      query?: {
        /** @format guid */
        cohortId?: string | null;
        search?: string | null;
      },
      params: RequestParams = {},
    ) =>
      this.request<DashboardSnapshotResponse, any>({
        path: `/api/dashboards/${id}`,
        method: "GET",
        query: query,
        format: "json",
        ...params,
      }),
    useDashboardsGet: (
      id: string,
      query?: {
        /** @format guid */
        cohortId?: string | null;
        search?: string | null;
      },
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<DashboardSnapshotResponse, any>(
        doFetch ? [`/api/dashboards/${id}`, query] : null,
        options,
      ),

    mutateDashboardsGet: (
      id: string,
      query?: {
        /** @format guid */
        cohortId?: string | null;
        search?: string | null;
      },
      data?: DashboardSnapshotResponse | Promise<DashboardSnapshotResponse>,
      options?: MutatorOptions,
    ) =>
      mutate<DashboardSnapshotResponse>(
        [`/api/dashboards/${id}`, query],
        data,
        options,
      ),
  };
  challengeInstances = {
    /**
     * No description
     *
     * @tags ChallengeInstances
     * @name ChallengeInstancesExtend
     * @request POST:/api/challenges/{challengeId}/instances/extend
     */
    challengeInstancesExtend: (
      challengeId: string,
      params: RequestParams = {},
    ) =>
      this.request<ChallengeInstanceResponse, any>({
        path: `/api/challenges/${challengeId}/instances/extend`,
        method: "POST",
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags ChallengeInstances
     * @name ChallengeInstancesGet
     * @request GET:/api/challenges/{challengeId}/instances
     */
    challengeInstancesGet: (challengeId: string, params: RequestParams = {}) =>
      this.request<ChallengeInstanceResponse, any>({
        path: `/api/challenges/${challengeId}/instances`,
        method: "GET",
        format: "json",
        ...params,
      }),
    useChallengeInstancesGet: (
      challengeId: string,
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<ChallengeInstanceResponse, any>(
        doFetch ? `/api/challenges/${challengeId}/instances` : null,
        options,
      ),

    mutateChallengeInstancesGet: (
      challengeId: string,
      data?: ChallengeInstanceResponse | Promise<ChallengeInstanceResponse>,
      options?: MutatorOptions,
    ) =>
      mutate<ChallengeInstanceResponse>(
        `/api/challenges/${challengeId}/instances`,
        data,
        options,
      ),

    /**
     * No description
     *
     * @tags ChallengeInstances
     * @name ChallengeInstancesStart
     * @request POST:/api/challenges/{challengeId}/instances
     */
    challengeInstancesStart: (
      challengeId: string,
      params: RequestParams = {},
    ) =>
      this.request<ChallengeInstanceResponse, any>({
        path: `/api/challenges/${challengeId}/instances`,
        method: "POST",
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags ChallengeInstances
     * @name ChallengeInstancesStop
     * @request DELETE:/api/challenges/{challengeId}/instances
     */
    challengeInstancesStop: (challengeId: string, params: RequestParams = {}) =>
      this.request<Blob, any>({
        path: `/api/challenges/${challengeId}/instances`,
        method: "DELETE",
        ...params,
      }),
  };
  challenges = {
    /**
     * No description
     *
     * @tags Challenges
     * @name ChallengesDownloadAttachment
     * @request GET:/api/challenges/{id}/attachment
     */
    challengesDownloadAttachment: (id: string, params: RequestParams = {}) =>
      this.request<Blob, any>({
        path: `/api/challenges/${id}/attachment`,
        method: "GET",
        ...params,
      }),
    useChallengesDownloadAttachment: (
      id: string,
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<Blob, any>(
        doFetch ? `/api/challenges/${id}/attachment` : null,
        options,
      ),

    mutateChallengesDownloadAttachment: (
      id: string,
      data?: Blob | Promise<Blob>,
      options?: MutatorOptions,
    ) => mutate<Blob>(`/api/challenges/${id}/attachment`, data, options),

    /**
     * No description
     *
     * @tags Challenges
     * @name ChallengesGet
     * @request GET:/api/challenges/{id}
     */
    challengesGet: (
      id: string,
      query?: {
        locale?: string | null;
      },
      params: RequestParams = {},
    ) =>
      this.request<ChallengeRuntimeDetailResponse, any>({
        path: `/api/challenges/${id}`,
        method: "GET",
        query: query,
        format: "json",
        ...params,
      }),
    useChallengesGet: (
      id: string,
      query?: {
        locale?: string | null;
      },
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<ChallengeRuntimeDetailResponse, any>(
        doFetch ? [`/api/challenges/${id}`, query] : null,
        options,
      ),

    mutateChallengesGet: (
      id: string,
      query?: {
        locale?: string | null;
      },
      data?:
        | ChallengeRuntimeDetailResponse
        | Promise<ChallengeRuntimeDetailResponse>,
      options?: MutatorOptions,
    ) =>
      mutate<ChallengeRuntimeDetailResponse>(
        [`/api/challenges/${id}`, query],
        data,
        options,
      ),

    /**
     * No description
     *
     * @tags Challenges
     * @name ChallengesNextHint
     * @request GET:/api/challenges/{id}/hints/next
     */
    challengesNextHint: (
      id: string,
      query?: {
        locale?: string | null;
      },
      params: RequestParams = {},
    ) =>
      this.request<ChallengeHintResponse, any>({
        path: `/api/challenges/${id}/hints/next`,
        method: "GET",
        query: query,
        format: "json",
        ...params,
      }),
    useChallengesNextHint: (
      id: string,
      query?: {
        locale?: string | null;
      },
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<ChallengeHintResponse, any>(
        doFetch ? [`/api/challenges/${id}/hints/next`, query] : null,
        options,
      ),

    mutateChallengesNextHint: (
      id: string,
      query?: {
        locale?: string | null;
      },
      data?: ChallengeHintResponse | Promise<ChallengeHintResponse>,
      options?: MutatorOptions,
    ) =>
      mutate<ChallengeHintResponse>(
        [`/api/challenges/${id}/hints/next`, query],
        data,
        options,
      ),

    /**
     * No description
     *
     * @tags Challenges
     * @name ChallengesWriteup
     * @request GET:/api/challenges/{id}/writeup
     */
    challengesWriteup: (
      id: string,
      query?: {
        locale?: string | null;
      },
      params: RequestParams = {},
    ) =>
      this.request<ChallengeWriteupResponse, any>({
        path: `/api/challenges/${id}/writeup`,
        method: "GET",
        query: query,
        format: "json",
        ...params,
      }),
    useChallengesWriteup: (
      id: string,
      query?: {
        locale?: string | null;
      },
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<ChallengeWriteupResponse, any>(
        doFetch ? [`/api/challenges/${id}/writeup`, query] : null,
        options,
      ),

    mutateChallengesWriteup: (
      id: string,
      query?: {
        locale?: string | null;
      },
      data?: ChallengeWriteupResponse | Promise<ChallengeWriteupResponse>,
      options?: MutatorOptions,
    ) =>
      mutate<ChallengeWriteupResponse>(
        [`/api/challenges/${id}/writeup`, query],
        data,
        options,
      ),
  };
  challengeSubmissions = {
    /**
     * No description
     *
     * @tags ChallengeSubmissions
     * @name ChallengeSubmissionsHistory
     * @request GET:/api/challenges/{challengeId}/submissions
     */
    challengeSubmissionsHistory: (
      challengeId: string,
      params: RequestParams = {},
    ) =>
      this.request<Blob, any>({
        path: `/api/challenges/${challengeId}/submissions`,
        method: "GET",
        ...params,
      }),
    useChallengeSubmissionsHistory: (
      challengeId: string,
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<Blob, any>(
        doFetch ? `/api/challenges/${challengeId}/submissions` : null,
        options,
      ),

    mutateChallengeSubmissionsHistory: (
      challengeId: string,
      data?: Blob | Promise<Blob>,
      options?: MutatorOptions,
    ) =>
      mutate<Blob>(`/api/challenges/${challengeId}/submissions`, data, options),

    /**
     * No description
     *
     * @tags ChallengeSubmissions
     * @name ChallengeSubmissionsSubmit
     * @request POST:/api/challenges/{challengeId}/submissions
     */
    challengeSubmissionsSubmit: (
      challengeId: string,
      data: ChallengeSubmissionRequest,
      params: RequestParams = {},
    ) =>
      this.request<ChallengeSubmissionResult, any>({
        path: `/api/challenges/${challengeId}/submissions`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),
  };
  adminChallenges = {
    /**
     * No description
     *
     * @tags AdminChallenges
     * @name AdminChallengesCreate
     * @request POST:/api/admin/challenges
     */
    adminChallengesCreate: (
      data: ChallengeCommand,
      params: RequestParams = {},
    ) =>
      this.request<ChallengeEditResponse, any>({
        path: `/api/admin/challenges`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags AdminChallenges
     * @name AdminChallengesDelete
     * @request DELETE:/api/admin/challenges/{id}
     */
    adminChallengesDelete: (id: string, params: RequestParams = {}) =>
      this.request<Blob, any>({
        path: `/api/admin/challenges/${id}`,
        method: "DELETE",
        ...params,
      }),

    /**
     * No description
     *
     * @tags AdminChallenges
     * @name AdminChallengesGet
     * @request GET:/api/admin/challenges/{id}
     */
    adminChallengesGet: (
      id: string,
      query?: {
        locale?: string | null;
      },
      params: RequestParams = {},
    ) =>
      this.request<ChallengeSummaryResponse, any>({
        path: `/api/admin/challenges/${id}`,
        method: "GET",
        query: query,
        format: "json",
        ...params,
      }),
    useAdminChallengesGet: (
      id: string,
      query?: {
        locale?: string | null;
      },
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<ChallengeSummaryResponse, any>(
        doFetch ? [`/api/admin/challenges/${id}`, query] : null,
        options,
      ),

    mutateAdminChallengesGet: (
      id: string,
      query?: {
        locale?: string | null;
      },
      data?: ChallengeSummaryResponse | Promise<ChallengeSummaryResponse>,
      options?: MutatorOptions,
    ) =>
      mutate<ChallengeSummaryResponse>(
        [`/api/admin/challenges/${id}`, query],
        data,
        options,
      ),

    /**
     * No description
     *
     * @tags AdminChallenges
     * @name AdminChallengesGetForEdit
     * @request GET:/api/admin/challenges/{id}/edit
     */
    adminChallengesGetForEdit: (
      id: string,
      query?: {
        locale?: string | null;
      },
      params: RequestParams = {},
    ) =>
      this.request<ChallengeEditResponse, any>({
        path: `/api/admin/challenges/${id}/edit`,
        method: "GET",
        query: query,
        format: "json",
        ...params,
      }),
    useAdminChallengesGetForEdit: (
      id: string,
      query?: {
        locale?: string | null;
      },
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<ChallengeEditResponse, any>(
        doFetch ? [`/api/admin/challenges/${id}/edit`, query] : null,
        options,
      ),

    mutateAdminChallengesGetForEdit: (
      id: string,
      query?: {
        locale?: string | null;
      },
      data?: ChallengeEditResponse | Promise<ChallengeEditResponse>,
      options?: MutatorOptions,
    ) =>
      mutate<ChallengeEditResponse>(
        [`/api/admin/challenges/${id}/edit`, query],
        data,
        options,
      ),

    /**
     * No description
     *
     * @tags AdminChallenges
     * @name AdminChallengesList
     * @request GET:/api/admin/challenges
     */
    adminChallengesList: (
      query?: {
        locale?: string | null;
      },
      params: RequestParams = {},
    ) =>
      this.request<ChallengeSummaryResponse[], any>({
        path: `/api/admin/challenges`,
        method: "GET",
        query: query,
        format: "json",
        ...params,
      }),
    useAdminChallengesList: (
      query?: {
        locale?: string | null;
      },
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<ChallengeSummaryResponse[], any>(
        doFetch ? [`/api/admin/challenges`, query] : null,
        options,
      ),

    mutateAdminChallengesList: (
      query?: {
        locale?: string | null;
      },
      data?: ChallengeSummaryResponse[] | Promise<ChallengeSummaryResponse[]>,
      options?: MutatorOptions,
    ) =>
      mutate<ChallengeSummaryResponse[]>(
        [`/api/admin/challenges`, query],
        data,
        options,
      ),

    /**
     * No description
     *
     * @tags AdminChallenges
     * @name AdminChallengesMerge
     * @request POST:/api/admin/challenges/{id}/merge
     */
    adminChallengesMerge: (
      id: string,
      data: MergeChallengeCommand,
      params: RequestParams = {},
    ) =>
      this.request<ChallengeMergeResult, any>({
        path: `/api/admin/challenges/${id}/merge`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags AdminChallenges
     * @name AdminChallengesPublish
     * @request POST:/api/admin/challenges/{id}/publish
     */
    adminChallengesPublish: (
      id: string,
      data: PublishContentCommand,
      params: RequestParams = {},
    ) =>
      this.request<Blob, any>({
        path: `/api/admin/challenges/${id}/publish`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        ...params,
      }),

    /**
     * No description
     *
     * @tags AdminChallenges
     * @name AdminChallengesUpdate
     * @request PUT:/api/admin/challenges/{id}
     */
    adminChallengesUpdate: (
      id: string,
      data: ChallengeCommand,
      params: RequestParams = {},
    ) =>
      this.request<ChallengeEditResponse, any>({
        path: `/api/admin/challenges/${id}`,
        method: "PUT",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),
  };
  adminLessons = {
    /**
     * No description
     *
     * @tags AdminLessons
     * @name AdminLessonsCreate
     * @request POST:/api/admin/lessons
     */
    adminLessonsCreate: (data: LessonCommand, params: RequestParams = {}) =>
      this.request<LessonResponse, any>({
        path: `/api/admin/lessons`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags AdminLessons
     * @name AdminLessonsDelete
     * @request DELETE:/api/admin/lessons/{id}
     */
    adminLessonsDelete: (id: string, params: RequestParams = {}) =>
      this.request<Blob, any>({
        path: `/api/admin/lessons/${id}`,
        method: "DELETE",
        ...params,
      }),

    /**
     * No description
     *
     * @tags AdminLessons
     * @name AdminLessonsGet
     * @request GET:/api/admin/lessons/{id}
     */
    adminLessonsGet: (
      id: string,
      query?: {
        locale?: string | null;
      },
      params: RequestParams = {},
    ) =>
      this.request<LessonResponse, any>({
        path: `/api/admin/lessons/${id}`,
        method: "GET",
        query: query,
        format: "json",
        ...params,
      }),
    useAdminLessonsGet: (
      id: string,
      query?: {
        locale?: string | null;
      },
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<LessonResponse, any>(
        doFetch ? [`/api/admin/lessons/${id}`, query] : null,
        options,
      ),

    mutateAdminLessonsGet: (
      id: string,
      query?: {
        locale?: string | null;
      },
      data?: LessonResponse | Promise<LessonResponse>,
      options?: MutatorOptions,
    ) =>
      mutate<LessonResponse>(
        [`/api/admin/lessons/${id}`, query],
        data,
        options,
      ),

    /**
     * No description
     *
     * @tags AdminLessons
     * @name AdminLessonsList
     * @request GET:/api/admin/lessons
     */
    adminLessonsList: (
      query?: {
        locale?: string | null;
      },
      params: RequestParams = {},
    ) =>
      this.request<LessonResponse[], any>({
        path: `/api/admin/lessons`,
        method: "GET",
        query: query,
        format: "json",
        ...params,
      }),
    useAdminLessonsList: (
      query?: {
        locale?: string | null;
      },
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<LessonResponse[], any>(
        doFetch ? [`/api/admin/lessons`, query] : null,
        options,
      ),

    mutateAdminLessonsList: (
      query?: {
        locale?: string | null;
      },
      data?: LessonResponse[] | Promise<LessonResponse[]>,
      options?: MutatorOptions,
    ) => mutate<LessonResponse[]>([`/api/admin/lessons`, query], data, options),

    /**
     * No description
     *
     * @tags AdminLessons
     * @name AdminLessonsPublish
     * @request POST:/api/admin/lessons/{id}/publish
     */
    adminLessonsPublish: (
      id: string,
      data: PublishContentCommand,
      params: RequestParams = {},
    ) =>
      this.request<Blob, any>({
        path: `/api/admin/lessons/${id}/publish`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        ...params,
      }),

    /**
     * No description
     *
     * @tags AdminLessons
     * @name AdminLessonsUpdate
     * @request PUT:/api/admin/lessons/{id}
     */
    adminLessonsUpdate: (
      id: string,
      data: LessonCommand,
      params: RequestParams = {},
    ) =>
      this.request<LessonResponse, any>({
        path: `/api/admin/lessons/${id}`,
        method: "PUT",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),
  };
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
    accountChangePassword: (
      data: PasswordChangeModel,
      params: RequestParams = {},
    ) =>
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
    accountMailChangeConfirm: (
      data: AccountVerifyModel,
      params: RequestParams = {},
    ) =>
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
    accountPasswordReset: (
      data: PasswordResetModel,
      params: RequestParams = {},
    ) =>
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
    useAccountProfile: (options?: SWRConfiguration, doFetch: boolean = true) =>
      useSWR<ProfileUserInfoModel, RequestResponse>(
        doFetch ? `/api/account/profile` : null,
        options,
      ),

    mutateAccountProfile: (
      data?: ProfileUserInfoModel | Promise<ProfileUserInfoModel>,
      options?: MutatorOptions,
    ) => mutate<ProfileUserInfoModel>(`/api/account/profile`, data, options),

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
    ) =>
      useSWR<ArrayResponseOfLocalFile, RequestResponse>(
        doFetch ? [`/api/admin/files`, query] : null,
        options,
      ),

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
    ) =>
      mutate<ArrayResponseOfLocalFile>(
        [`/api/admin/files`, query],
        data,
        options,
      ),

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
    useAdminGetConfigs: (options?: SWRConfiguration, doFetch: boolean = true) =>
      useSWR<ConfigEditModel, RequestResponse>(
        doFetch ? `/api/admin/config` : null,
        options,
      ),

    mutateAdminGetConfigs: (
      data?: ConfigEditModel | Promise<ConfigEditModel>,
      options?: MutatorOptions,
    ) => mutate<ConfigEditModel>(`/api/admin/config`, data, options),

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
    useAdminInstances: (options?: SWRConfiguration, doFetch: boolean = true) =>
      useSWR<ArrayResponseOfContainerInstanceModel, RequestResponse>(
        doFetch ? `/api/admin/instances` : null,
        options,
      ),

    mutateAdminInstances: (
      data?:
        | ArrayResponseOfContainerInstanceModel
        | Promise<ArrayResponseOfContainerInstanceModel>,
      options?: MutatorOptions,
    ) =>
      mutate<ArrayResponseOfContainerInstanceModel>(
        `/api/admin/instances`,
        data,
        options,
      ),

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
    ) =>
      useSWR<LogMessageModel[], RequestResponse>(
        doFetch ? [`/api/admin/logs`, query] : null,
        options,
      ),

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
     * @description Use this API to modify user information, requires Admin permission
     *
     * @tags Admin
     * @name AdminUpdateUserInfo
     * @summary Modify user information
     * @request PUT:/api/admin/users/{userid}
     */
    adminUpdateUserInfo: (
      userid: string,
      data: AdminUserInfoModel,
      params: RequestParams = {},
    ) =>
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
    useAdminUserInfo: (
      userid: string,
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<ProfileUserInfoModel, RequestResponse>(
        doFetch ? `/api/admin/users/${userid}` : null,
        options,
      ),

    mutateAdminUserInfo: (
      userid: string,
      data?: ProfileUserInfoModel | Promise<ProfileUserInfoModel>,
      options?: MutatorOptions,
    ) =>
      mutate<ProfileUserInfoModel>(`/api/admin/users/${userid}`, data, options),

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
    ) =>
      useSWR<ArrayResponseOfUserInfoModel, RequestResponse>(
        doFetch ? [`/api/admin/users`, query] : null,
        options,
      ),

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
      data?:
        | ArrayResponseOfUserInfoModel
        | Promise<ArrayResponseOfUserInfoModel>,
      options?: MutatorOptions,
    ) =>
      mutate<ArrayResponseOfUserInfoModel>(
        [`/api/admin/users`, query],
        data,
        options,
      ),
  };
  apiToken = {
    /**
     * No description
     *
     * @tags ApiToken
     * @name ApiTokenGenerateToken
     * @summary Generates a new API token.
     * @request POST:/api/tokens
     */
    apiTokenGenerateToken: (
      data: ApiTokenCreateModel,
      params: RequestParams = {},
    ) =>
      this.request<ApiTokenResponse, RequestResponse>({
        path: `/api/tokens`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags ApiToken
     * @name ApiTokenListTokens
     * @summary Lists all API tokens.
     * @request GET:/api/tokens
     */
    apiTokenListTokens: (params: RequestParams = {}) =>
      this.request<ApiToken[], RequestResponse>({
        path: `/api/tokens`,
        method: "GET",
        format: "json",
        ...params,
      }),
    useApiTokenListTokens: (
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<ApiToken[], RequestResponse>(
        doFetch ? `/api/tokens` : null,
        options,
      ),

    mutateApiTokenListTokens: (
      data?: ApiToken[] | Promise<ApiToken[]>,
      options?: MutatorOptions,
    ) => mutate<ApiToken[]>(`/api/tokens`, data, options),

    /**
     * No description
     *
     * @tags ApiToken
     * @name ApiTokenRestoreToken
     * @summary Restores an API token.
     * @request POST:/api/tokens/{id}/restore
     */
    apiTokenRestoreToken: (id: string, params: RequestParams = {}) =>
      this.request<void, RequestResponse | ProblemDetails>({
        path: `/api/tokens/${id}/restore`,
        method: "POST",
        ...params,
      }),

    /**
     * No description
     *
     * @tags ApiToken
     * @name ApiTokenRevokeToken
     * @summary Revokes an API token.
     * @request DELETE:/api/tokens/{id}
     */
    apiTokenRevokeToken: (
      id: string,
      query?: {
        /**
         * If true, the token will be deleted instead of just revoked.
         * @default false
         */
        delete?: boolean;
      },
      params: RequestParams = {},
    ) =>
      this.request<void, RequestResponse | ProblemDetails>({
        path: `/api/tokens/${id}`,
        method: "DELETE",
        query: query,
        ...params,
      }),
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
    assetsGetFile: (
      hash: string,
      filename: string,
      params: RequestParams = {},
    ) =>
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
  editPosts = {
    /**
     * No description
     *
     * @tags EditPosts
     * @name EditPostsCreate
     * @request POST:/api/edit/posts
     */
    editPostsCreate: (data: PostEditModel, params: RequestParams = {}) =>
      this.request<string, any>({
        path: `/api/edit/posts`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags EditPosts
     * @name EditPostsDelete
     * @request DELETE:/api/edit/posts/{id}
     */
    editPostsDelete: (id: string, params: RequestParams = {}) =>
      this.request<Blob, any>({
        path: `/api/edit/posts/${id}`,
        method: "DELETE",
        ...params,
      }),

    /**
     * No description
     *
     * @tags EditPosts
     * @name EditPostsUpdate
     * @request PUT:/api/edit/posts/{id}
     */
    editPostsUpdate: (
      id: string,
      data: PostEditModel,
      params: RequestParams = {},
    ) =>
      this.request<PostDetailModel, any>({
        path: `/api/edit/posts/${id}`,
        method: "PUT",
        body: data,
        type: ContentType.Json,
        format: "json",
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
    useInfoGetClientCaptchaInfo: (
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<ClientCaptchaInfoModel, any>(
        doFetch ? `/api/captcha` : null,
        options,
      ),

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
    useInfoGetClientConfig: (
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) => useSWR<ClientConfig, any>(doFetch ? `/api/config` : null, options),

    mutateInfoGetClientConfig: (
      data?: ClientConfig | Promise<ClientConfig>,
      options?: MutatorOptions,
    ) => mutate<ClientConfig>(`/api/config`, data, options),

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
    useInfoGetLatestPosts: (
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<PostInfoModel[], any>(
        doFetch ? `/api/posts/latest` : null,
        options,
      ),

    mutateInfoGetLatestPosts: (
      data?: PostInfoModel[] | Promise<PostInfoModel[]>,
      options?: MutatorOptions,
    ) => mutate<PostInfoModel[]>(`/api/posts/latest`, data, options),

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
    useInfoGetPost: (
      id: string,
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<PostDetailModel, RequestResponse>(
        doFetch ? `/api/posts/${id}` : null,
        options,
      ),

    mutateInfoGetPost: (
      id: string,
      data?: PostDetailModel | Promise<PostDetailModel>,
      options?: MutatorOptions,
    ) => mutate<PostDetailModel>(`/api/posts/${id}`, data, options),

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
    useInfoGetPosts: (options?: SWRConfiguration, doFetch: boolean = true) =>
      useSWR<PostInfoModel[], any>(doFetch ? `/api/posts` : null, options),

    mutateInfoGetPosts: (
      data?: PostInfoModel[] | Promise<PostInfoModel[]>,
      options?: MutatorOptions,
    ) => mutate<PostInfoModel[]>(`/api/posts`, data, options),

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
    useInfoPowChallenge: (
      options?: SWRConfiguration,
      doFetch: boolean = true,
    ) =>
      useSWR<HashPowChallenge, RequestResponse>(
        doFetch ? `/api/captcha/powchallenge` : null,
        options,
      ),

    mutateInfoPowChallenge: (
      data?: HashPowChallenge | Promise<HashPowChallenge>,
      options?: MutatorOptions,
    ) => mutate<HashPowChallenge>(`/api/captcha/powchallenge`, data, options),
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
}

const api = new Api();
export default api;

export const fetcher = async (
  args: string | [string, Record<string, unknown>],
) => {
  if (typeof args === "string") {
    const response = await api.request({ path: args, format: "json" });
    return response.data;
  } else {
    const [path, query] = args;
    const response = await api.request({ path, query, format: "json" });
    return response.data;
  }
};
