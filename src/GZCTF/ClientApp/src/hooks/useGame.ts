import dayjs from 'dayjs'
import { TFunction } from 'i18next'
import { GameStatus } from '@Components/GameCard'
import { OnceSWRConfig } from '@Hooks/useConfig'
import api, { ParticipationStatus } from '@Api'

export const useRecentGames = () => {
  const { data, mutate, error } = api.game.useGameRecentGames(
    { limit: 7 },
    {
      refreshInterval: 30 * 60 * 1000,
    }
  )

  return { recentGames: data, error, mutate }
}

export const getGameStatus = (game?: { start?: number; end?: number }) => {
  const startTime = dayjs(game?.start)
  const endTime = dayjs(game?.end)

  const total = endTime.diff(startTime, 'minute')
  const current = dayjs().diff(startTime, 'minute')

  const finished = dayjs().isAfter(endTime)
  const started = dayjs().isAfter(startTime)
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

export const toLimitTag = (t: TFunction, limit?: number) => {
  if (!limit || limit === 0) return t('game.tag.multiplayer')
  if (limit === 1) return t('game.tag.individual')
  return t('game.tag.limited', { count: limit })
}

export const useAdminGame = (numId: number) => {
  const { data: game, mutate, error } = api.edit.useEditGetGame(numId, OnceSWRConfig)

  return { game, error, mutate }
}

export const useGame = (numId: number) => {
  const { data: game, error, mutate } = api.game.useGameGame(numId, OnceSWRConfig)

  return { game, error, mutate, status: game?.status ?? ParticipationStatus.Unsubmitted }
}

export const useGameScoreboard = (numId: number) => {
  const { game } = useGame(numId)
  const { status } = getGameStatus(game)

  const {
    data: scoreboard,
    error,
    mutate,
  } = api.game.useGameScoreboard(numId, {
    ...OnceSWRConfig,
    refreshInterval: status === GameStatus.OnGoing ? 30 * 1000 : 0,
  })

  return { scoreboard, error, mutate }
}

export const useGameTeamInfo = (numId: number) => {
  const { game } = useGame(numId)
  const { status } = getGameStatus(game)

  const {
    data: teamInfo,
    error,
    mutate,
  } = api.game.useGameChallengesWithTeamInfo(numId, {
    ...OnceSWRConfig,
    shouldRetryOnError: false,
    refreshInterval: status === GameStatus.OnGoing ? 10 * 1000 : 0,
  })

  return { teamInfo, error, mutate }
}
