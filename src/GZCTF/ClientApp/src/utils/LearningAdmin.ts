import type {
  ChallengeLocalizationCommand,
  ChallengeLocalizationResponse,
  LessonLocalizationCommand,
  LessonLocalizationResponse,
} from '@Api'

const mergeLocalization = <T extends { locale?: string }>(existing: T[], edited: T): T[] => {
  const locale = edited.locale?.toLocaleLowerCase()
  const index = existing.findIndex((item) => item.locale?.toLocaleLowerCase() === locale)
  if (index < 0) return [...existing, edited]
  return existing.map((item, itemIndex) => (itemIndex === index ? edited : item))
}

export const mergeChallengeLocalization = (
  existing: ChallengeLocalizationResponse[],
  edited: ChallengeLocalizationCommand,
): ChallengeLocalizationCommand[] => mergeLocalization(existing, edited)

export const mergeLessonLocalization = (
  existing: LessonLocalizationResponse[],
  edited: LessonLocalizationCommand,
): LessonLocalizationCommand[] => mergeLocalization(existing, edited)
