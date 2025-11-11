import { Box, Button, Center, ScrollArea, Stack, Text, Title } from '@mantine/core'
import { useClipboard } from '@mantine/hooks'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiClose, mdiPlus } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC, useEffect, useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate, useParams } from 'react-router'
import { DivisionCard } from '@Components/admin/DivisionCard'
import { DivisionEditDrawer } from '@Components/admin/DivisionEditDrawer'
import { WithGameEditTab } from '@Components/admin/WithGameEditTab'
import { showErrorMsg } from '@Utils/Shared'
import { useEditChallenges } from '@Hooks/useEdit'
import { useAdminDivisions } from '@Hooks/useGame'
import api, { Division } from '@Api'
import classes from '@Styles/Divisions.module.css'

const GameDivisionManagement: FC = () => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1', 10)

  const { t } = useTranslation()
  const navigate = useNavigate()
  const clipboard = useClipboard({ timeout: 1500 })

  const { divisions, mutate: mutateDivisions } = useAdminDivisions(numId)
  const { challenges } = useEditChallenges(numId)

  const [modalOpened, setModalOpened] = useState(false)
  const [activeDivision, setActiveDivision] = useState<Division | null>(null)

  useEffect(() => {
    if (Number.isNaN(numId) || numId < 0) {
      showNotification({
        color: 'red',
        message: t('common.error.param_error'),
        icon: <Icon path={mdiClose} size={1} />,
      })
      navigate('/admin/games')
    }
  }, [navigate, numId, t])

  const challengeTitleMap = useMemo(() => {
    const map = new Map<number, string>()
    challenges?.forEach((challenge) => {
      if (challenge.id) {
        map.set(challenge.id, challenge.title)
      }
    })
    return map
  }, [challenges, t])

  const sortedDivisions = useMemo(
    () => (divisions ? divisions.toSorted((a, b) => a.name.localeCompare(b.name)) : []),
    [divisions]
  )

  const handleCopyInviteCode = (code: string) => {
    if (!code) return
    try {
      clipboard.copy(code)
      showNotification({
        color: 'teal',
        message: t('admin.notification.games.divisions.invite_copied'),
        icon: <Icon path={mdiCheck} size={1} />,
      })
    } catch {
      showNotification({
        color: 'red',
        message: t('common.error.try_later'),
        icon: <Icon path={mdiClose} size={1} />,
      })
    }
  }

  const handleDivisionSaved = (division: Division) => {
    const current = divisions ?? []
    const index = current.findIndex((item) => item.id === division.id)
    const next =
      index === -1 ? [...current, division] : current.map((item) => (item.id === division.id ? division : item))
    mutateDivisions(next, false)
    setActiveDivision(null)
  }

  const handleDeleteDivision = async (division: Division) => {
    try {
      await api.edit.editDeleteDivision(numId, division.id)
      showNotification({
        color: 'teal',
        message: t('admin.notification.games.divisions.deleted'),
        icon: <Icon path={mdiCheck} size={1} />,
      })
      const next = (divisions ?? []).filter((item) => item.id !== division.id)
      mutateDivisions(next, false)
    } catch (error) {
      showErrorMsg(error, t)
    }
  }

  const openCreateModal = () => {
    setActiveDivision(null)
    setModalOpened(true)
  }

  const isLoading = divisions === undefined || challenges === null

  return (
    <WithGameEditTab
      isLoading={isLoading}
      contentPos="flex-end"
      head={
        <Button mr="18px" leftSection={<Icon path={mdiPlus} size={1} />} onClick={openCreateModal}>
          {t('admin.button.divisions.new')}
        </Button>
      }
    >
      <ScrollArea h="calc(100vh - 180px)" offsetScrollbars type="auto">
        {sortedDivisions.length === 0 ? (
          <Center h="calc(100vh - 200px)">
            <Stack gap={0}>
              <Title order={2}>{t('admin.content.games.divisions.empty.title')}</Title>
              <Text>{t('admin.content.games.divisions.empty.description')}</Text>
            </Stack>
          </Center>
        ) : (
          <Box className={classes.masonryContainer}>
            {sortedDivisions.map((division) => (
              <DivisionCard
                key={division.id}
                division={division}
                challengeTitleMap={challengeTitleMap}
                onEdit={(selected) => {
                  setActiveDivision(selected)
                  setModalOpened(true)
                }}
                onDelete={handleDeleteDivision}
                onCopyInviteCode={handleCopyInviteCode}
                className={classes.masonryItem}
              />
            ))}
          </Box>
        )}
      </ScrollArea>
      <DivisionEditDrawer
        title={activeDivision ? t('admin.button.divisions.edit') : t('admin.button.divisions.new')}
        opened={modalOpened}
        onClose={() => {
          setModalOpened(false)
          setActiveDivision(null)
        }}
        gameId={numId}
        division={activeDivision}
        challenges={challenges}
        onDivisionSaved={handleDivisionSaved}
      />
    </WithGameEditTab>
  )
}

export default GameDivisionManagement
