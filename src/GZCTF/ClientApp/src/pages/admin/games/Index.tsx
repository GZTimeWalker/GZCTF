import { ActionIcon, Avatar, Badge, Button, Code, Group, Paper, ScrollArea, Switch, Table, Text } from '@mantine/core'
import { mdiArrowLeftBold, mdiArrowRightBold, mdiChevronTripleRight, mdiPencilOutline, mdiPlus } from '@mdi/js'
import { Icon } from '@mdi/react'
import dayjs from 'dayjs'
import { FC, useEffect, useState } from 'react'
import { Trans, useTranslation } from 'react-i18next'
import { Link, useNavigate } from 'react-router'
import { GameColorMap } from '@Components/GameCard'
import { AdminPage } from '@Components/admin/AdminPage'
import { GameCreateModal } from '@Components/admin/GameCreateModal'
import { showErrorNotification } from '@Utils/ApiHelper'
import { useArrayResponse } from '@Hooks/useArrayResponse'
import { getGameStatus } from '@Hooks/useGame'
import api, { GameInfoModel } from '@Api'
import misc from '@Styles/Misc.module.css'
import tableClasses from '@Styles/Table.module.css'

const ITEM_COUNT_PER_PAGE = 30

const Games: FC = () => {
  const [page, setPage] = useState(1)
  const [createOpened, setCreateOpened] = useState(false)
  const [disabled, setDisabled] = useState(false)
  const { data: games, total, setData: setGames, updateData: updateGames } = useArrayResponse<GameInfoModel>()
  const [current, setCurrent] = useState(0)

  const navigate = useNavigate()
  const { t } = useTranslation()

  const onToggleHidden = async (game: GameInfoModel) => {
    if (!game.id) return
    setDisabled(true)

    try {
      await api.edit.editUpdateGame(game.id, {
        ...game,
        hidden: !game.hidden,
      })
      if (games) {
        updateGames(
          games.map((g) => {
            if (g.id === game.id) {
              return { ...g, hidden: !g.hidden }
            }
            return g
          })
        )
      }
    } catch (e) {
      showErrorNotification(e, t)
    } finally {
      setDisabled(false)
    }
  }

  useEffect(() => {
    const fetchData = async () => {
      try {
        const res = await api.edit.editGetGames({
          count: ITEM_COUNT_PER_PAGE,
          skip: (page - 1) * ITEM_COUNT_PER_PAGE,
        })
        setGames(res.data)
        setCurrent((page - 1) * ITEM_COUNT_PER_PAGE + res.data.length)
      } catch (e) {
        showErrorNotification(e, t)
      }
    }

    fetchData()
  }, [page])

  return (
    <AdminPage
      isLoading={!games}
      headProps={{ justify: 'apart' }}
      head={
        <>
          <Button leftSection={<Icon path={mdiPlus} size={1} />} onClick={() => setCreateOpened(true)}>
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
            <ActionIcon size="lg" disabled={page * ITEM_COUNT_PER_PAGE >= total} onClick={() => setPage(page + 1)}>
              <Icon path={mdiArrowRightBold} size={1} />
            </ActionIcon>
          </Group>
        </>
      }
    >
      <Paper shadow="md" p="md" w="100%">
        <ScrollArea offsetScrollbars h="calc(100vh - 190px)">
          <Table className={tableClasses.table}>
            <Table.Thead>
              <Table.Tr>
                <Table.Th miw="1.8rem">{t('admin.label.games.hide')}</Table.Th>
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
                      <Table.Td>
                        <Switch disabled={disabled} checked={game.hidden} onChange={() => onToggleHidden(game)} />
                      </Table.Td>
                      <Table.Td>
                        <Group wrap="nowrap" justify="space-between">
                          <Group
                            wrap="nowrap"
                            justify="left"
                            onClick={() => navigate(`/games/${game.id}`)}
                            className={misc.cPointer}
                          >
                            <Avatar alt="avatar" src={game.poster} radius={0}>
                              {game.title?.slice(0, 1)}
                            </Avatar>
                            <Text fw="bold" lineClamp={1} maw="calc(10vw)">
                              {game.title}
                            </Text>
                          </Group>
                          <Badge color={color}>{status}</Badge>
                        </Group>
                      </Table.Td>
                      <Table.Td>
                        <Group wrap="nowrap" gap="xs">
                          <Badge size="xs" color={color} variant="dot">
                            {dayjs(startTime).format('YYYY-MM-DD HH:mm')}
                          </Badge>
                          <Icon path={mdiChevronTripleRight} size={1} />
                          <Badge size="xs" color={color} variant="dot">
                            {dayjs(endTime).format('YYYY-MM-DD HH:mm')}
                          </Badge>
                        </Group>
                      </Table.Td>
                      <Table.Td>
                        <Text size="sm" truncate maw="20rem">
                          {game.summary}
                        </Text>
                      </Table.Td>
                      <Table.Td>
                        <Group justify="right">
                          <ActionIcon component={Link} to={`/admin/games/${game.id}/info`}>
                            <Icon path={mdiPencilOutline} size={1} />
                          </ActionIcon>
                        </Group>
                      </Table.Td>
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
