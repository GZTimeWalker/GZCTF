import { Stack } from '@mantine/core'
import { FC } from 'react'
import { useTranslation } from 'react-i18next'
import { GameCard } from '@Components/GameCard'
import { WithNavBar } from '@Components/WithNavbar'
import { OnceSWRConfig } from '@Hooks/useConfig'
import { usePageTitle } from '@Hooks/usePageTitle'
import api from '@Api'

const Games: FC = () => {
  const { t } = useTranslation()

  const { data: games } = api.game.useGameGames({ count: 50, skip: 0 }, OnceSWRConfig)

  usePageTitle(t('game.title.index'))

  // TODO: Gantt chart + timeline + waterfall view

  return (
    <WithNavBar withHeader stickyHeader>
      <Stack>{games && games.data.map((g) => <GameCard key={g.id} game={g} />)}</Stack>
    </WithNavBar>
  )
}

export default Games
