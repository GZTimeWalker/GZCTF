import {
  ChallengeCategory,
  ChallengeFlagKind,
  ChallengeType,
  Difficulty,
  type ChallengeCommand,
  type ChallengeEditResponse,
} from '@Api'
import { mergeChallengeLocalization } from '@Utils/LearningAdmin'

export type AttachmentEntry = {
  FileName: string
  Sha256: string
  StorageKey: string
  Flag: string
}

export type ContainerFields = {
  ContainerImage: string
  ExposedPort: number
  Cpu: number
  MemoryMb: number
  StorageMb: number
  NetworkMode: string
  FlagTemplate: string
}

export type ChallengeDraft = {
  title: string
  summary: string
  body: string
  type: ChallengeType
  ctfCategory: ChallengeCategory
  difficulty: Difficulty
  expectedMinutes: number
  submissionLimit: number
  isEnabled: boolean
  container: ContainerFields
  attachments: AttachmentEntry[]
  staticFlags: string[]
  hints: string[]
  writeup: string
}

export const isContainerType = (type: ChallengeType) =>
  type === ChallengeType.StaticContainer || type === ChallengeType.DynamicContainer

export const isAttachmentType = (type: ChallengeType) =>
  type === ChallengeType.StaticAttachment || type === ChallengeType.DynamicAttachment

const defaultContainer: ContainerFields = {
  ContainerImage: '', ExposedPort: 80, Cpu: 1, MemoryMb: 256,
  StorageMb: 512, NetworkMode: 'Open', FlagTemplate: '',
}

const parseConfig = (value?: string | null): Record<string, unknown> => {
  if (!value) return {}
  try {
    const parsed: unknown = JSON.parse(value)
    return parsed && typeof parsed === 'object' && !Array.isArray(parsed)
      ? parsed as Record<string, unknown>
      : {}
  } catch {
    return {}
  }
}

const parseAttachments = (value?: string | null): AttachmentEntry[] => {
  if (!value) return []
  try {
    const parsed: unknown = JSON.parse(value)
    const entries = Array.isArray(parsed) ? parsed :
      parsed && typeof parsed === 'object' ? (parsed as Record<string, unknown>).Attachments : []
    if (!Array.isArray(entries)) return []
    return entries.map((entry) => {
      const item = entry as Record<string, unknown>
      return {
        FileName: String(item.FileName ?? item.fileName ?? ''),
        Sha256: String(item.Sha256 ?? item.sha256 ?? ''),
        StorageKey: String(item.StorageKey ?? item.storageKey ?? item.FileName ?? ''),
        Flag: String(item.Flag ?? item.flag ?? ''),
      }
    })
  } catch {
    return []
  }
}

export const challengeDraftFromResponse = (data: ChallengeEditResponse): ChallengeDraft => {
  const text = data.localizations?.find((item) => item.locale?.toLowerCase() === 'en')
  const type = data.challenge?.type ?? ChallengeType.StaticAttachment
  const config = parseConfig(data.runtimeConfigurationJson)
  const dynamicPool = data.flags?.find((flag) => flag.kind === ChallengeFlagKind.DynamicAttachment)
  const template = data.flags?.find((flag) => flag.kind === ChallengeFlagKind.Template)?.template
  return {
    title: text?.title ?? data.challenge?.title ?? '',
    summary: text?.summary ?? data.challenge?.summary ?? '',
    body: text?.body ?? '',
    type,
    ctfCategory: data.challenge?.ctfCategory ?? ChallengeCategory.Misc,
    difficulty: data.challenge?.difficulty ?? Difficulty.Normal,
    expectedMinutes: data.challenge?.expectedMinutes ?? 60,
    submissionLimit: data.challenge?.submissionLimit ?? 0,
    isEnabled: data.challenge?.isEnabled ?? true,
    container: {
      ContainerImage: String(config.ContainerImage ?? config.Image ?? ''),
      ExposedPort: Number(config.ExposedPort ?? 80),
      Cpu: Number(config.Cpu ?? 1),
      MemoryMb: Number(config.MemoryMb ?? 256),
      StorageMb: Number(config.StorageMb ?? 512),
      NetworkMode: String(config.NetworkMode ?? 'Open'),
      FlagTemplate: String(config.FlagTemplate ?? template ?? ''),
    },
    attachments: parseAttachments(dynamicPool?.metadataJson ?? data.runtimeConfigurationJson),
    staticFlags: data.flags?.filter((flag) => flag.kind === ChallengeFlagKind.Static)
      .map((flag) => flag.value ?? '') ?? [],
    hints: data.hints?.filter((hint) => hint.locale?.toLowerCase() === 'en')
      .sort((left, right) => (left.sortOrder ?? 0) - (right.sortOrder ?? 0))
      .map((hint) => hint.content ?? '') ?? [],
    writeup: data.writeups?.find((item) => item.locale?.toLowerCase() === 'en')?.content ?? '',
  }
}

export const challengeCommandFromDraft = (
  draft: ChallengeDraft,
  current: ChallengeEditResponse,
  rowVersion: number | undefined,
): ChallengeCommand => {
  const attachments = draft.attachments.map((entry) => ({
    ...entry,
    Flag: draft.type === ChallengeType.StaticAttachment ? draft.staticFlags[0] ?? '' : entry.Flag,
  }))
  const flags = draft.type === ChallengeType.DynamicContainer
    ? [{ kind: ChallengeFlagKind.Template, template: draft.container.FlagTemplate }]
    : draft.type === ChallengeType.DynamicAttachment
      ? [{ kind: ChallengeFlagKind.DynamicAttachment, metadataJson: JSON.stringify(attachments) }]
      : draft.staticFlags.filter((value) => value.trim()).map((value) => ({ kind: ChallengeFlagKind.Static, value }))
  return {
    rowVersion,
    type: draft.type,
    ctfCategory: draft.ctfCategory,
    difficulty: draft.difficulty,
    isEnabled: draft.isEnabled,
    expectedMinutes: draft.expectedMinutes,
    submissionLimit: draft.submissionLimit,
    runtimeConfigurationJson: isContainerType(draft.type)
      ? JSON.stringify(draft.container)
      : JSON.stringify({ Attachments: attachments }),
    localizations: mergeChallengeLocalization(current.localizations ?? [], {
      locale: 'en', title: draft.title.trim(), summary: draft.summary.trim(), body: draft.body,
    }),
    flags,
    hints: draft.hints.filter((content) => content.trim()).map((content, sortOrder) => ({
      locale: 'en', sortOrder, content,
    })),
    writeups: draft.writeup.trim() ? [{ locale: 'en', content: draft.writeup }] : [],
  }
}
