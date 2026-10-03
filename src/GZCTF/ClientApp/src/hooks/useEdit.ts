import { useMemo } from 'react'
import { OnceSWRConfig } from '@Hooks/useConfig'
import api, { ChallengeInfoModel } from '@Api'

export const useEditChallenge = (numId: number, numCId: number) => {
  const { data: challenge, error, mutate } = api.edit.useEditGetGameChallenge(numId, numCId, OnceSWRConfig)

  return { challenge, error, mutate }
}

export const useEditChallenges = (numId: number) => {
  const { data, error, mutate } = api.edit.useEditGetGameChallenges(numId, OnceSWRConfig)

  const sortedChallenges = useMemo<ChallengeInfoModel[] | null>(
    () => data?.toSorted((a, b) => ((a.category ?? '') > (b.category ?? '') ? -1 : 1)) ?? null,
    [data]
  )

  return { challenges: sortedChallenges, error, mutate }
}
