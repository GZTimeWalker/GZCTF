import {
  Button,
  Card,
  Center,
  Divider,
  Group,
  ScrollArea,
  SimpleGrid,
  Skeleton,
  Stack,
  Switch,
  Tabs,
  Text,
  Title,
} from '@mantine/core'
import { useLocalStorage } from '@mantine/hooks'
import { mdiFileUploadOutline, mdiFlagOutline, mdiPuzzle } from '@mdi/js'
import { Icon } from '@mdi/react'
import dayjs from 'dayjs'
import React, { FC, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useLocation, useParams } from 'react-router'
import { ChallengeCard } from '@Components/ChallengeCard'
import { Empty } from '@Components/Empty'
import { GameChallengeModal } from '@Components/GameChallengeModal'
import { WriteupSubmitModal } from '@Components/WriteupSubmitModal'
import { useChallengeCategoryLabelMap, SubmissionTypeIconMap } from '@Utils/Shared'
import { useGame, useGameTeamInfo } from '@Hooks/useGame'
import { ChallengeInfo, ChallengeCategory, SubmissionType } from '@Api'
import classes from '@Styles/ChallengePanel.module.css'

export const ChallengePanel: FC = () => {
  const { hash } = useLocation()
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')

  const { teamInfo } = useGameTeamInfo(numId)
  const challenges = teamInfo?.challenges

  const { game } = useGame(numId)

  const categories = Object.keys(challenges ?? {})
  const [activeTab, setActiveTab] = useState<ChallengeCategory | 'All'>('All')
  const [hideSolved, setHideSolved] = useLocalStorage({
    key: 'hide-solved',
    defaultValue: false,
    getInitialValueInEffect: false,
  })

  const allChallenges = Object.values(challenges ?? {}).flat()

  const currentChallenges =
    challenges &&
    (activeTab !== 'All' ? (challenges[activeTab] ?? []) : allChallenges).filter(
      (chal) =>
        !hideSolved || (teamInfo && teamInfo.rank?.solvedChallenges?.find((c) => c.id === chal.id)) === undefined
    )

  const [challenge, setChallenge] = useState<ChallengeInfo | null>(null)
  const [detailOpened, setDetailOpened] = useState(false)
  const { iconMap, colorMap } = SubmissionTypeIconMap(0.8)
  const [writeupSubmitOpened, setWriteupSubmitOpened] = useState(false)
  const challengeCategoryLabelMap = useChallengeCategoryLabelMap()
  const { t } = useTranslation()

  useEffect(() => {
    const challId = hash.slice(1).split('-')[0]
    if (challId && allChallenges) {
      const id = parseInt(challId)
      if (isNaN(id) || id < 0) return
      if (challenge?.id === id) return

      const chal = allChallenges.find((c) => c.id === id)
      if (chal) {
        setChallenge(chal)
        setDetailOpened(true)
      }
    }
  }, [hash, challenge, allChallenges])

  // skeleton for loading
  if (!challenges) {
    return (
      <>
        <Stack miw="10rem" maw="10rem">
          {Array(9)
            .fill(null)
            .map((_v, i) => (
              <Group key={i} wrap="nowrap" p={10}>
                <Skeleton height="1.5rem" width="1.5rem" />
                <Skeleton height="1rem" />
              </Group>
            ))}
        </Stack>
        <SimpleGrid
          p="xs"
          pt={0}
          spacing="sm"
          pos="relative"
          w="calc(100% - 9rem)"
          cols={{ base: 3, w18: 4, w24: 6, w30: 8, w36: 10, w42: 12, w48: 14 }}
        >
          {Array(13)
            .fill(null)
            .map((_v, i) => (
              <Card key={i} radius="md" shadow="sm">
                <Stack gap="sm" pos="relative" style={{ zIndex: 99 }}>
                  <Skeleton height="1.5rem" width="70%" mt={4} />
                  <Divider />
                  <Group wrap="nowrap" justify="space-between" align="start">
                    <Center>
                      <Skeleton height="1.5rem" width="5rem" />
                    </Center>
                    <Stack gap="xs">
                      <Skeleton height="1rem" width="6rem" mt={5} />
                      <Group justify="center" gap="md" h={20}>
                        <Skeleton height="1.2rem" width="1.2rem" />
                        <Skeleton height="1.2rem" width="1.2rem" />
                        <Skeleton height="1.2rem" width="1.2rem" />
                      </Group>
                    </Stack>
                  </Group>
                </Stack>
              </Card>
            ))}
        </SimpleGrid>
      </>
    )
  }

  if (allChallenges.length === 0) {
    return (
      <Center h="calc(100vh - 100px)" w="100%">
        <Empty
          bordered
          description={t('game.content.no_challenge')}
          fontSize="xl"
          mdiPath={mdiFlagOutline}
          iconSize={8}
        />
      </Center>
    )
  }

  return (
    <>
      <Stack miw="10rem">
        {game?.writeupRequired && (
          <>
            <Button
              px="xs"
              leftSection={<Icon path={mdiFileUploadOutline} size={1} />}
              onClick={() => setWriteupSubmitOpened(true)}
            >
              {t('game.button.submit_writeup')}
            </Button>
            <Divider />
          </>
        )}
        <Switch
          w="10rem"
          checked={hideSolved}
          onChange={(e) => setHideSolved(e.target.checked)}
          classNames={{ body: classes.switch }}
          label={
            <Text fz="md" fw="bold">
              {t('game.button.hide_solved')}
            </Text>
          }
        />
        <Tabs
          orientation="vertical"
          variant="pills"
          value={activeTab}
          onChange={(value) => setActiveTab(value as ChallengeCategory)}
          classNames={{
            list: classes.tabList,
            tabLabel: classes.tabLabel,
            tab: classes.tab,
          }}
        >
          <Tabs.List>
            <Tabs.Tab value={'All'} leftSection={<Icon path={mdiPuzzle} size={1} />}>
              <Group justify="space-between" wrap="nowrap" gap={2}>
                <Text fz="sm" fw="bold">
                  All
                </Text>
                <Text fz="sm" fw="bold">
                  {allChallenges.length}
                </Text>
              </Group>
            </Tabs.Tab>
            {categories.map((tab) => {
              const data = challengeCategoryLabelMap.get(tab as ChallengeCategory)!
              return (
                <Tabs.Tab key={tab} value={tab} leftSection={<Icon path={data?.icon} size={1} />} color={data?.color}>
                  <Group justify="space-between" wrap="nowrap" gap={2}>
                    <Text fz="sm" fw="bold">
                      {data?.name}
                    </Text>
                    <Text fz="sm" fw="bold">
                      {challenges && challenges[tab].length}
                    </Text>
                  </Group>
                </Tabs.Tab>
              )
            })}
          </Tabs.List>
        </Tabs>
      </Stack>
      <ScrollArea
        h="calc(100vh - 6.67rem)"
        pos="relative"
        offsetScrollbars
        scrollbarSize={4}
        classNames={{ root: classes.scrollArea }}
      >
        {/* if rank is 0, means scoreboard not ready yet */}
        {!teamInfo?.rank?.rank ? (
          <Center h="calc(100vh - 10rem)">
            <Stack gap={0}>
              <Title order={2}>{t('game.content.scoreboard_not_ready.title')}</Title>
              <Text>{t('game.content.scoreboard_not_ready.comment')}</Text>
            </Stack>
          </Center>
        ) : currentChallenges && currentChallenges.length ? (
          <SimpleGrid
            p="xs"
            w="100%"
            pt={0}
            spacing="sm"
            cols={{ base: 3, w18: 4, w24: 6, w30: 8, w36: 10, w42: 12, w48: 14 }}
          >
            {currentChallenges?.map((chal) => {
              const status = teamInfo?.rank?.solvedChallenges?.find((c) => c.id === chal.id)?.type
              const solved = status !== SubmissionType.Unaccepted && status !== undefined

              return (
                <ChallengeCard
                  key={chal.id}
                  challenge={chal}
                  iconMap={iconMap}
                  colorMap={colorMap}
                  onClick={() => {
                    setChallenge(chal)
                    setDetailOpened(true)
                    // update hash after modal opened, so don't trigger useEffect
                    window.location.hash = `#${chal.id}-${encodeURIComponent(chal.title?.replace(/ /g, '-') ?? '')}`
                  }}
                  solved={solved}
                  teamId={teamInfo?.rank?.id}
                />
              )
            })}
          </SimpleGrid>
        ) : (
          <Center h="calc(100vh - 10rem)">
            <Stack gap={0}>
              <Title order={2}>{t('game.content.all_solved.title')}</Title>
              <Text>{t('game.content.all_solved.comment')}</Text>
            </Stack>
          </Center>
        )}
      </ScrollArea>
      {game?.writeupRequired && (
        <WriteupSubmitModal
          opened={writeupSubmitOpened}
          onClose={() => setWriteupSubmitOpened(false)}
          withCloseButton={false}
          size="40%"
          gameId={numId}
          writeupDeadline={teamInfo.writeupDeadline}
        />
      )}
      {challenge?.id && (
        <GameChallengeModal
          gameId={numId}
          opened={detailOpened}
          withCloseButton={false}
          onClose={() => {
            window.location.hash = ''
            setDetailOpened(false)
          }}
          gameEnded={dayjs(game?.end) < dayjs()}
          status={teamInfo?.rank?.solvedChallenges?.find((c) => c.id === challenge?.id)?.type}
          cateData={
            challengeCategoryLabelMap.get((challenge?.category as ChallengeCategory) ?? ChallengeCategory.Misc)!
          }
          title={challenge?.title ?? ''}
          score={challenge?.score ?? 0}
          challengeId={challenge.id}
        />
      )}
    </>
  )
}
