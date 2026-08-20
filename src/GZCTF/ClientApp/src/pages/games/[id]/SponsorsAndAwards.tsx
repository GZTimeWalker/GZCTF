import { Card, Container, Group, Image, Paper, SimpleGrid, Stack, Text, Title } from '@mantine/core'
import { FC, useEffect, useState } from 'react'
import { useParams } from 'react-router'
import { showErrorMsg } from '@Utils/Shared'
import cyctfApi, { AwardResponse, SponsorResponse } from '@/CyctfApi'

const GameSponsorsAndAwards: FC = () => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')

  const [sponsors, setSponsors] = useState<SponsorResponse[]>([])
  const [awards, setAwards] = useState<AwardResponse[]>([])

  useEffect(() => {
    if (numId > 0) {
      loadData()
    }
  }, [numId])

  const loadData = async () => {
    try {
      const [sponsorsRes, awardsRes] = await Promise.all([
        cyctfApi.sponsor.list(numId),
        cyctfApi.award.list(numId),
      ])
      setSponsors(sponsorsRes.data.sort((a, b) => a.sortOrder - b.sortOrder))
      setAwards(awardsRes.data.sort((a, b) => a.sortOrder - b.sortOrder))
    } catch (err) {
      showErrorMsg(err)
    }
  }

  const groupedSponsors = sponsors.reduce((acc, sponsor) => {
    const type = sponsor.typeLabel || sponsor.type
    if (!acc[type]) {
      acc[type] = []
    }
    acc[type].push(sponsor)
    return acc
  }, {} as Record<string, SponsorResponse[]>)

  return (
    <Container size="xl" py="xl">
      <Stack gap="xl">
        {/* 赞助商区域 */}
        {sponsors.length > 0 && (
          <Stack gap="lg">
            <Title order={2} ta="center">
              赞助商
            </Title>

            {Object.entries(groupedSponsors).map(([type, typeSponsors]) => (
              <Stack key={type} gap="md">
                <Title order={3} ta="center" c="dimmed" size="h4">
                  {type}
                </Title>

                <SimpleGrid
                  cols={{ base: 2, sm: 3, md: 4, lg: 5 }}
                  spacing="lg"
                >
                  {typeSponsors.map((sponsor) => (
                    <Card
                      key={sponsor.id}
                      shadow="sm"
                      padding="lg"
                      component={sponsor.website ? 'a' : 'div'}
                      href={sponsor.website || undefined}
                      target="_blank"
                      style={{
                        cursor: sponsor.website ? 'pointer' : 'default',
                        transition: 'transform 0.2s',
                      }}
                      onMouseEnter={(e) => {
                        if (sponsor.website) {
                          e.currentTarget.style.transform = 'translateY(-4px)'
                        }
                      }}
                      onMouseLeave={(e) => {
                        e.currentTarget.style.transform = 'translateY(0)'
                      }}
                    >
                      <Stack gap="sm" align="center">
                        {sponsor.logoUrl ? (
                          <Image
                            src={sponsor.logoUrl}
                            alt={sponsor.fullName || sponsor.shortName}
                            height={80}
                            fit="contain"
                          />
                        ) : (
                          <Paper
                            p="xl"
                            bg="var(--mantine-color-gray-1)"
                            style={{ width: '100%', height: 80 }}
                          >
                            <Text ta="center" fw={600} size="lg">
                              {sponsor.shortName}
                            </Text>
                          </Paper>
                        )}
                        <Text ta="center" size="sm" lineClamp={2}>
                          {sponsor.fullName || sponsor.shortName}
                        </Text>
                      </Stack>
                    </Card>
                  ))}
                </SimpleGrid>
              </Stack>
            ))}
          </Stack>
        )}

        {/* 奖项区域 */}
        {awards.length > 0 && (
          <Stack gap="lg">
            <Title order={2} ta="center">
              奖项
            </Title>

            <SimpleGrid cols={{ base: 1, sm: 2, md: 3 }} spacing="lg">
              {awards.map((award) => (
                <Card
                  key={award.id}
                  shadow="sm"
                  padding="lg"
                  style={{
                    background: `linear-gradient(135deg, ${award.primaryColor || '#3B6DFF'}22 0%, ${award.secondaryColor || '#6EE7B7'}22 100%)`,
                    borderLeft: `4px solid ${award.primaryColor || '#3B6DFF'}`,
                  }}
                >
                  <Stack gap="md">
                    <Group justify="space-between" align="flex-start">
                      <Title order={4}>{award.name}</Title>
                      <div
                        style={{
                          width: 32,
                          height: 32,
                          borderRadius: '50%',
                          background: `linear-gradient(135deg, ${award.primaryColor || '#3B6DFF'}, ${award.secondaryColor || '#6EE7B7'})`,
                        }}
                      />
                    </Group>

                    {award.description && (
                      <Text size="sm" c="dimmed">
                        {award.description}
                      </Text>
                    )}

                    {award.teams && award.teams.length > 0 && (
                      <Stack gap="xs">
                        <Text size="sm" fw={500}>
                          获奖队伍:
                        </Text>
                        {award.teams.map((team) => (
                          <Paper key={team.teamId} p="xs" bg="white">
                            <Text size="sm">{team.teamName}</Text>
                          </Paper>
                        ))}
                      </Stack>
                    )}
                  </Stack>
                </Card>
              ))}
            </SimpleGrid>
          </Stack>
        )}

        {sponsors.length === 0 && awards.length === 0 && (
          <Paper p="xl" bg="var(--mantine-color-gray-0)">
            <Text ta="center" c="dimmed">
              暂无赞助商和奖项信息
            </Text>
          </Paper>
        )}
      </Stack>
    </Container>
  )
}

export default GameSponsorsAndAwards
