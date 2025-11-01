import {
  ActionIcon,
  Avatar,
  Badge,
  Button,
  Code,
  FileButton,
  Group,
  Paper,
  Progress,
  ScrollArea,
  Switch,
  Table,
  Text,
  alpha,
  useMantineTheme,
} from '@mantine/core'
import {
  mdiArrowLeftBold,
  mdiArrowRightBold,
  mdiChevronTripleRight,
  mdiPencilOutline,
  mdiPlus,
  mdiUpload,
} from '@mdi/js'
import { Icon } from '@mdi/react'
import dayjs from 'dayjs'
import { FC, useEffect, useState } from 'react'
import { Trans, useTranslation } from 'react-i18next'
import { Link, useNavigate } from 'react-router'
import { GameColorMap } from '@Components/GameCard'
import { AdminPage } from '@Components/admin/AdminPage'
import { GameCreateModal } from '@Components/admin/GameCreateModal'
import { showErrorMsg } from '@Utils/Shared'
import { useArrayResponse } from '@Hooks/useArrayResponse'
import { getGameStatus } from '@Hooks/useGame'
import api, { GameInfoModel } from '@Api'
import misc from '@Styles/Misc.module.css'
import tableClasses from '@Styles/Table.module.css'
import uploadClasses from '@Styles/Upload.module.css'

const ITEM_COUNT_PER_PAGE = 30

const Games: FC = () => {
  const [page, setPage] = useState(1)
  const [createOpened, setCreateOpened] = useState(false)
  const [disabled, setDisabled] = useState(false)
  const [progress, setProgress] = useState(0)
  const { data: games, total, setData: setGames, updateData: updateGames } = useArrayResponse<GameInfoModel>()
  const [current, setCurrent] = useState(0)

  const navigate = useNavigate()
  const { t } = useTranslation()
  const theme = useMantineTheme()

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
      showErrorMsg(e, t)
    } finally {
      setDisabled(false)
    }
  }

  const onImportGame = async (file: File | null) => {
    if (!file) return

    setProgress(0)
    setDisabled(true)

    try {
      const res = await api.edit.editImportGame(
        { file },
        {
          onUploadProgress: (e) => {
            setProgress((e.loaded / (e.total ?? 1)) * 100)
          },
        }
      )

      setProgress(0)
      setDisabled(false)

      if (res.data) {
        // Refresh the games list
        const gamesRes = await api.edit.editGetGames({
          count: ITEM_COUNT_PER_PAGE,
          skip: (page - 1) * ITEM_COUNT_PER_PAGE,
        })
        setGames(gamesRes.data)
        setCurrent((page - 1) * ITEM_COUNT_PER_PAGE + gamesRes.data.length)

        // Navigate to the imported game
        navigate(`/admin/games/${res.data}/info`)
      }
    } catch (err) {
      showErrorMsg(err, t)
      setProgress(0)
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
        showErrorMsg(e, t)
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
          <Group gap="md" wrap="nowrap">
            <Button leftSection={<Icon path={mdiPlus} size={1} />} onClick={() => setCreateOpened(true)}>
              {t('admin.button.games.new')}
            </Button>
            <FileButton onChange={onImportGame} accept="application/zip">
              {(props) => (
                <Button
                  {...props}
                  leftSection={<Icon path={mdiUpload} size={1} />}
                  className={uploadClasses.button}
                  disabled={disabled}
                  color={progress !== 0 ? 'cyan' : theme.primaryColor}
                  variant="outline"
                >
                  <div className={uploadClasses.label}>
                    {progress !== 0 ? t('admin.notification.games.import.importing') : t('admin.button.games.import')}
                  </div>
                  {progress !== 0 && (
                    <Progress
                      value={progress}
                      className={uploadClasses.progress}
                      color={alpha(theme.colors[theme.primaryColor][2], 0.35)}
                      radius="sm"
                    />
                  )}
                </Button>
              )}
            </FileButton>
          </Group>
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
                            <Text fw="bold" lineClamp={1} maw="calc(20vw)">
                              {game.title}
                            </Text>
                          </Group>
                          <Badge color={color}>{status}</Badge>
                        </Group>
                      </Table.Td>
                      <Table.Td>
                        <Group wrap="nowrap" gap="xs">
                          <Badge size="sm" color={color} variant="dot">
                            {dayjs(startTime).format('YYYY-MM-DD HH:mm')}
                          </Badge>
                          <Icon path={mdiChevronTripleRight} size={1} />
                          <Badge size="sm" color={color} variant="dot">
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
