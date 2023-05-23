import api, { ParticipationStatus } from '@Api'

export const useGame = (numId: number) => {
  const {
    data: game,
    error,
    mutate,
  } = api.game.useGameGames(numId, {
    refreshInterval: 0,
    revalidateOnFocus: false,
  })

  return { game, error, mutate, status: game?.status ?? ParticipationStatus.Unsubmitted }
}
