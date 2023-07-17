import api from '@Api'
import { OnceSWRConfig } from './useConfig'

export const useEditChallenge = (numId: number, numCId: number) => {
  const {
    data: challenge,
    error,
    mutate,
  } = api.edit.useEditGetGameChallenge(numId, numCId, OnceSWRConfig)

  return { challenge, error, mutate }
}
