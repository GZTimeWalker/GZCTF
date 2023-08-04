import { OnceSWRConfig } from '@Utils/useConfig'
import api from '@Api'

export const useEditChallenge = (numId: number, numCId: number) => {
  const {
    data: challenge,
    error,
    mutate,
  } = api.edit.useEditGetGameChallenge(numId, numCId, OnceSWRConfig)

  return { challenge, error, mutate }
}
