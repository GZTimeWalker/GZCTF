import {
  ActionIcon,
  Avatar,
  Badge,
  Button,
  Code,
  Group,
  Paper,
  ScrollArea,
  Switch,
  Table,
  Text,
} from '@mantine/core'
import {
  mdiArrowLeftBold,
  mdiArrowRightBold,
  mdiChevronTripleRight,
  mdiPencilOutline,
  mdiPlus,
} from '@mdi/js'
import { Icon } from '@mdi/react'
import dayjs from 'dayjs'
import { FC, useEffect, useState } from 'react'
import { Trans, useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import { GameColorMap } from '@Components/GameCard'
import AdminPage from '@Components/admin/AdminPage'
import GameCreateModal from '@Components/admin/GameCreateModal'
import { showErrorNotification } from '@Utils/ApiHelper'
import { useTableStyles } from '@Utils/ThemeOverride'
import { useArrayResponse } from '@Utils/useArrayResponse'
import { getGameStatus } from '@Utils/useGame'
import api, { GameInfoModel } from '@Api'

const ITEM_COUNT_PER_PAGE = 30

const Games: FC = () => {
  const [page, setPage] = useState(1)
  const [createOpened, setCreateOpened] = useState(false)
  const [disabled, setDisabled] = useState(false)
  const {
    data: games,
    total,
    setData: setGames,
    updateData: updateGames,
  } = useArrayResponse<GameInfoModel>()
  const [current, setCurrent] = useState(0)

  const navigate = useNavigate()
  const { classes } = useTableStyles()
  const { t } = useTranslation()

  const onToggleHidden = (game: GameInfoModel) => {
    if (!game.id) return

    setDisabled(true)
    api.edit
      .editUpdateGame(game.id, {
        ...game,
        hidden: !game.hidden,
      })
      .then(() => {
        games && updateGames(games.map((g) => (g.id === game.id ? { ...g, hidden: !g.hidden } : g)))
      })
      .catch((e) => showErrorNotification(e, t))
      .finally(() => setDisabled(false))
  }

  useEffect(() => {
    api.edit
      .editGetGames({
        count: ITEM_COUNT_PER_PAGE,
        skip: (page - 1) * ITEM_COUNT_PER_PAGE,
      })
      .then((res) => {
        setGames(res.data)
        setCurrent((page - 1) * ITEM_COUNT_PER_PAGE + res.data.length)
      })
  }, [page])

  return (
    <AdminPage
      isLoading={!games}
      headProps={{ justify: 'apart' }}
      head={
        <>
          <Button
            leftSection={<Icon path={mdiPlus} size={1} />}
            onClick={() => setCreateOpened(true)}
          >
            {t('admin.button.games.new')}
          </Button>
          <Group w="calc(100% - 9rem)" justify="right">
            <Text fw="bold" size="sm">
              <Trans
                i18nKey="admin.content.games.stats"
                values={{
                  current,
                  total,
                }}
              >
                _<Code>_</Code>_
              </Trans>
            </Text>
            <ActionIcon size="lg" disabled={page <= 1} onClick={() => setPage(page - 1)}>
              <Icon path={mdiArrowLeftBold} size={1} />
            </ActionIcon>
            <Text fw="bold" size="sm">
              {page}
            </Text>
            <ActionIcon
              size="lg"
              disabled={page * ITEM_COUNT_PER_PAGE >= total}
              onClick={() => setPage(page + 1)}
            >
              <Icon path={mdiArrowRightBold} size={1} />
            </ActionIcon>
          </Group>
        </>
      }
    >
      <Paper shadow="md" p="md" w="100%">
        <ScrollArea offsetScrollbars h="calc(100vh - 190px)">
          <Table className={classes.table}>
            <Table.Thead>
              <Table.Tr>
                <Table.Th>{t('admin.label.games.public')}</Table.Th>
                <Table.Th>{t('common.label.game')}</Table.Th>
                <Table.Th>{t('common.label.time')}</Table.Th>
                <Table.Th>{t('admin.label.games.summary')}</Table.Th>
                <Table.Th />
              </Table.Tr>
            </Table.Thead>
            <Table.Tbody>
              {games &&
                games.map((game) => {
                  const { startTime, endTime, status } = getGameStatus(game)
                  const color = GameColorMap.get(status)

                  return (
                    <Table.Tr key={game.id}>
                      <td>
                        <Switch
                          disabled={disabled}
                          checked={!game.hidden}
                          onChange={() => onToggleHidden(game)}
                        />
                      </td>
                      <td>
                        <Group wrap="nowrap" justify="space-between">
                          <Group
                            wrap="nowrap"
                            justify="left"
                            onClick={() => navigate(`/games/${game.id}`)}
                            sx={{ cursor: 'pointer' }}
                          >
                            <Avatar alt="avatar" src={game.poster} radius={0}>
                              {game.title?.slice(0, 1)}
                            </Avatar>
                            <Text fw={700} lineClamp={1} maw="calc(10vw)">
                              {game.title}
                            </Text>
                          </Group>
                          <Badge color={color}>{status}</Badge>
                        </Group>
                      </td>
                      <td>
                        <Group wrap="nowrap" gap="xs">
                          <Badge size="xs" color={color} variant="dot">
                            {dayjs(startTime).format('YYYY-MM-DD HH:mm')}
                          </Badge>
                          <Icon path={mdiChevronTripleRight} size={1} />
                          <Badge size="xs" color={color} variant="dot">
                            {dayjs(endTime).format('YYYY-MM-DD HH:mm')}
                          </Badge>
                        </Group>
                      </td>
                      <td>
                        <Text truncate maw="30rem">
                          {game.summary}
                        </Text>
                      </td>
                      <td>
                        <Group justify="right">
                          <ActionIcon
                            onClick={() => {
                              navigate(`/admin/games/${game.id}/info`)
                            }}
                          >
                            <Icon path={mdiPencilOutline} size={1} />
                          </ActionIcon>
                        </Group>
                      </td>
                    </Table.Tr>
                  )
                })}
            </Table.Tbody>
          </Table>
        </ScrollArea>
      </Paper>
      <GameCreateModal
        opened={createOpened}
        onClose={() => setCreateOpened(false)}
        onAddGame={(game) => updateGames([...(games ?? []), game])}
      />
    </AdminPage>
  )
}

export default Games
