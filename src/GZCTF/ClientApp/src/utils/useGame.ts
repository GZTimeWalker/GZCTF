import dayjs from 'dayjs'
import api, { DetailedGameInfoModel, ParticipationStatus } from '@Api'

export const getGameStatus = (game?: DetailedGameInfoModel) => {
  const startTime = dayjs(game?.start)
  const endTime = dayjs(game?.end)

  const duriation = endTime.diff(startTime, 'minute')
  const current = dayjs().diff(startTime, 'minute')

  const finished = current > duriation
  const started = current > 0
  const progress = started ? (finished ? 100 : current / duriation) : 0

  return {
    startTime,
    endTime,
    finished,
    started,
    progress,
  }
}

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

export const useGameScoreboard = (numId: number) => {
  const { game } = useGame(numId)

  const { finished, started } = getGameStatus(game)

  const inProgress = started && !finished

  const {
    data: scoreboard,
    error,
    mutate,
  } = api.game.useGameScoreboard(numId, {
    refreshInterval: inProgress ? 30 * 1000 : 0,
    revalidateOnFocus: false,
  })

  return { scoreboard, error, mutate }
}
