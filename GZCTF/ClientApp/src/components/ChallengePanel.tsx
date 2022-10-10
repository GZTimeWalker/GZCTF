import React, { FC, useState } from 'react'
import { useParams } from 'react-router-dom'
import {
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
import { mdiFlagOutline, mdiPuzzle } from '@mdi/js'
import { Icon } from '@mdi/react'
import api, { ChallengeInfo, ChallengeTag, SubmissionType } from '@Api'
import { ChallengeTagLabelMap, SubmissionTypeIconMap } from '../utils/ChallengeItem'
import ChallengeCard from './ChallengeCard'
import ChallengeDetailModal from './ChallengeDetailModal'
import Empty from './Empty'

const ChallengePanel: FC = () => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')

  const { data } = api.game.useGameChallengesWithTeamInfo(numId)
  const challenges = data?.challenges

  const tags = Object.keys(challenges ?? {})
  const [activeTab, setActiveTab] = useState<ChallengeTag | 'All'>('All')
  const [hideSolved, setHideSolved] = useLocalStorage({
    key: 'hide-solved',
    defaultValue: false,
    getInitialValueInEffect: false,
  })

  const allChallenges = Object.values(challenges ?? {}).flat()
  const currentChallenges =
    challenges &&
    (activeTab !== 'All' ? challenges[activeTab] ?? [] : allChallenges).filter(
      (chal) =>
        !hideSolved ||
        (data &&
          data.rank?.challenges?.find((c) => c.id === chal.id)?.type === SubmissionType.Unaccepted)
    )

  const [challenge, setChallenge] = useState<ChallengeInfo | null>(null)
  const [detailOpened, setDetailOpened] = useState(false)
  const { iconMap, colorMap } = SubmissionTypeIconMap(0.8)

  // skeleton for loading
  if (!challenges) {
    return (
      <Group
        spacing="sm"
        noWrap
        position="apart"
        align="flex-start"
        style={{ minWidth: 'calc(100% - 20rem)' }}
      >
        <Stack sx={{ minWidth: '10rem' }} spacing={6}>
          {Array(8)
            .fill(null)
            .map((_v, i) => (
              <Group key={i} noWrap p={10}>
                <Skeleton height="1.5rem" width="1.5rem" />
                <Skeleton height="1rem" />
              </Group>
            ))}
        </Stack>
        <SimpleGrid
          cols={3}
          spacing="sm"
          p="xs"
          style={{
            width: 'calc(100% - 9rem)',
            position: 'relative',
            paddingTop: 0,
          }}
          breakpoints={[
            { maxWidth: 2900, cols: 6 },
            { maxWidth: 2500, cols: 5 },
            { maxWidth: 2100, cols: 4 },
            { maxWidth: 1700, cols: 3 },
            { maxWidth: 1300, cols: 2 },
            { maxWidth: 900, cols: 1 },
          ]}
        >
          {Array(8)
            .fill(null)
            .map((_v, i) => (
              <Card key={i} radius="md" shadow="sm">
                <Stack spacing="sm" style={{ position: 'relative', zIndex: 99 }}>
                  <Skeleton height="1.5rem" width="70%" mt={4} />
                  <Divider />
                  <Group noWrap position="apart" align="start">
                    <Center>
                      <Skeleton height="1.5rem" width="5rem" />
                    </Center>
                    <Stack spacing="xs">
                      <Skeleton height="1rem" width="6rem" mt={5} />
                      <Group position="center" spacing="md" style={{ height: 20 }}>
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
      </Group>
    )
  }

  if (allChallenges.length === 0) {
    return (
      <Center sx={{ width: 'calc(100% - 20rem)', height: 'calc(100vh - 100px)' }}>
        <Empty
          bordered
          description="Ouch! 这个比赛还没有可用题目呢……"
          fontSize="xl"
          mdiPath={mdiFlagOutline}
          iconSize={8}
        />
      </Center>
    )
  }

  return (
    <Group
      spacing="sm"
      noWrap
      position="apart"
      align="flex-start"
      style={{ minWidth: 'calc(100% - 20rem)', maxWidth: 'calc(100% - 20rem)' }}
    >
      <Stack style={{ minWidth: '10rem' }}>
        <Switch
          checked={hideSolved}
          onChange={(e) => setHideSolved(e.target.checked)}
          label={
            <Text size="md" weight={700}>
              隐藏已解出
            </Text>
          }
        />
        <Tabs
          orientation="vertical"
          variant="pills"
          value={activeTab}
          onTabChange={(value) => setActiveTab(value as ChallengeTag)}
          styles={{
            tabsList: {
              minWidth: '10rem',
            },
            tab: {
              fontWeight: 700,
            },
            tabLabel: {
              width: '100%',
            },
          }}
        >
          <Tabs.List>
            <Tabs.Tab value={'All'} icon={<Icon path={mdiPuzzle} size={1} />}>
              <Group position="apart" noWrap spacing={2}>
                <Text>All</Text>
                <Text>{allChallenges.length}</Text>
              </Group>
            </Tabs.Tab>
            {tags.map((tab) => {
              const data = ChallengeTagLabelMap.get(tab as ChallengeTag)!
              return (
                <Tabs.Tab
                  key={tab}
                  value={tab}
                  icon={<Icon path={data?.icon} size={1} />}
                  color={data?.color}
                >
                  <Group position="apart" noWrap spacing={2}>
                    <Text>{data?.label}</Text>
                    <Text>{challenges && challenges[tab].length}</Text>
                  </Group>
                </Tabs.Tab>
              )
            })}
          </Tabs.List>
        </Tabs>
      </Stack>
      <ScrollArea
        style={{
          width: 'calc(100% - 9rem)',
          height: 'calc(100vh - 100px)',
          position: 'relative',
        }}
        offsetScrollbars
        scrollbarSize={4}
      >
        {currentChallenges && currentChallenges.length ? (
          <SimpleGrid
            cols={3}
            spacing="sm"
            p="xs"
            style={{ paddingTop: 0 }}
            breakpoints={[
              { maxWidth: 2900, cols: 6 },
              { maxWidth: 2500, cols: 5 },
              { maxWidth: 2100, cols: 4 },
              { maxWidth: 1700, cols: 3 },
              { maxWidth: 1300, cols: 2 },
              { maxWidth: 1080, cols: 1 },
            ]}
          >
            {currentChallenges?.map((chal) => (
              <ChallengeCard
                key={chal.id}
                challenge={chal}
                iconMap={iconMap}
                colorMap={colorMap}
                onClick={() => {
                  setChallenge(chal)
                  setDetailOpened(true)
                }}
                solved={
                  data &&
                  data.rank?.challenges?.find((c) => c.id === chal.id)?.type !==
                    SubmissionType.Unaccepted
                }
                teamId={data?.rank?.id}
              />
            ))}
          </SimpleGrid>
        ) : (
          <Stack spacing={0} pt="20vh" style={{ width: '20em', margin: 'auto' }}>
            <Title order={2}>题目都被解出啦！</Title>
            <Text>或许还有更难的挑战在等着你……</Text>
          </Stack>
        )}
      </ScrollArea>
      {challenge?.id && (
        <ChallengeDetailModal
          opened={detailOpened}
          onClose={() => setDetailOpened(false)}
          withCloseButton={false}
          size="40%"
          centered
          gameId={numId}
          solved={
            data &&
            data.rank?.challenges?.find((c) => c.id === challenge?.id)?.type !==
              SubmissionType.Unaccepted
          }
          tagData={ChallengeTagLabelMap.get((challenge?.tag as ChallengeTag) ?? ChallengeTag.Misc)!}
          title={challenge?.title ?? ''}
          score={challenge?.score ?? 0}
          challengeId={challenge.id}
        />
      )}
    </Group>
  )
}

export default ChallengePanel
