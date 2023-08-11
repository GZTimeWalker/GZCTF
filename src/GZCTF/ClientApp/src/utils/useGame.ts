import dayjs from 'dayjs'
import { GameStatus } from '@Components/GameCard'
import { OnceSWRConfig } from '@Utils/useConfig'
import api, { DetailedGameInfoModel, ParticipationStatus } from '@Api'

export const getGameStatus = (game?: DetailedGameInfoModel) => {
  const startTime = dayjs(game?.start)
  const endTime = dayjs(game?.end)

  const total = endTime.diff(startTime, 'minute')
  const current = dayjs().diff(startTime, 'minute')

  const finished = current > total
  const started = current > 0
  const progress = started ? (finished ? 1 : current / total) : 0
  const status = started ? (finished ? GameStatus.Ended : GameStatus.OnGoing) : GameStatus.Coming

  return {
    startTime,
    endTime,
    finished,
    started,
    progress: progress * 100,
    total,
    status,
  }
}

export const useGame = (numId: number) => {
  const { data: game, error, mutate } = api.game.useGameGames(numId, OnceSWRConfig)

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
    ...OnceSWRConfig,
    refreshInterval: inProgress ? 30 * 1000 : 0,
  })

  return { scoreboard, error, mutate }
}
